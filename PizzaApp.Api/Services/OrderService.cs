using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

public class OrderService(PizzaDbContext db, TimeProvider clock, IOptions<OrderingOptions> options, ICurrentUser user)
{
    private static readonly TimeZoneInfo Stockholm = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    private (DateOnly Date, bool Passed) CurrentDay()
    {
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Stockholm);
        return (DateOnly.FromDateTime(local.DateTime), TimeOnly.FromDateTime(local.DateTime) >= new TimeOnly(11, 15));
    }

    private async Task<Restaurant> RestaurantAsync(int id) =>
        await db.Restaurants.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id)
        ?? throw new OrderException(404, "Restaurangen finns inte.");

    private void CheckDeadline(Restaurant restaurant, bool passed)
    {
        if (restaurant.IsPizzeria && passed && options.Value.LockAfterDeadline)
            throw new OrderException(409, "Beställningarna är låsta efter kl. 11:15. Uppdatera listan.");
    }

    public async Task<DailyOrderList> GetTodayAsync(int restaurantId)
    {
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, passed) = CurrentDay();
        var completed = await db.CompletedOrderDays.AsNoTracking().SingleOrDefaultAsync(d => d.RestaurantId == restaurantId && d.Date == date);
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId && o.OrderDate == date)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToListAsync();
        return new()
        {
            Date = date, IsPizzeria = restaurant.IsPizzeria,
            CollectedAt = completed?.CollectedAt,
            CollectedBy = completed == null ? [] : JsonSerializer.Deserialize<List<string>>(completed.CollectorsJson)!,
            DeadlinePassed = restaurant.IsPizzeria && passed,
            IsLocked = completed != null || restaurant.IsPizzeria && passed && options.Value.LockAfterDeadline,
            Orders = orders.Select(Details).ToList(),
            Summary = orders.GroupBy(o => new { o.MenuItemId, o.Pizza, o.Sauce, o.Drink, o.Comment })
                .Select(g => new OrderSummary(g.Key.Pizza, g.Key.Sauce, g.Key.Drink, g.Key.Comment, g.Sum(o => o.Quantity)))
                .OrderBy(o => o.Pizza).ThenBy(o => o.Sauce).ThenBy(o => o.Drink).ThenBy(o => o.Comment).ToList()
        };
    }

    public async Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockRestaurantAsync(restaurantId);
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, passed) = CurrentDay();
        await CheckCompletedAsync(restaurantId, date);
        CheckDeadline(restaurant, passed);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new OrderException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
        if (restaurant.IsPizzeria && (!PizzaChoices.Sauces.Contains(input.Sauce) || !PizzaChoices.Drinks.Contains(input.Drink)))
            throw new OrderException(400, "Välj en sås (eller Ingen sås) och en dryck.");
        if (!restaurant.IsPizzeria && (input.Sauce != null || input.Drink != null))
            throw new OrderException(400, "À la carte har inga val för sås eller dryck.");
        var item = await db.MenuItems.AsNoTracking().SingleOrDefaultAsync(m => m.Id == input.MenuItemId && m.RestaurantId == restaurantId)
            ?? throw new OrderException(400, "Rätten finns inte på restaurangens meny.");
        var order = id.HasValue ? await EditableAsync(restaurantId, id.Value, date, input.Revision) : new PizzaOrder
        { OwnerUserId = user.Id, RestaurantId = restaurantId, OrderDate = date, CreatedAt = clock.GetUtcNow().UtcDateTime };
        if (!id.HasValue || order.MenuItemId != item.Id) order.UnitPrice = item.Price;
        if (!id.HasValue)
        {
            var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null || string.IsNullOrWhiteSpace(profile.DisplayName))
                throw new OrderException(409, "Välj ditt namn eller nick under Inställningar innan du beställer.");
            order.Name = profile.DisplayName;
        }
        order.MenuItemId = item.Id;
        order.Pizza = item.Name;
        order.Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim();
        order.Quantity = input.Quantity;
        order.Sauce = input.Sauce;
        order.Drink = input.Drink;
        order.Revision = Guid.NewGuid();
        if (!id.HasValue) db.Orders.Add(order);
        await PersistAsync();
        await transaction.CommitAsync();
        return Details(order);
    }

    public async Task DeleteAsync(int restaurantId, int id, Guid revision)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockRestaurantAsync(restaurantId);
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, passed) = CurrentDay();
        await CheckCompletedAsync(restaurantId, date);
        CheckDeadline(restaurant, passed);
        var order = await EditableAsync(restaurantId, id, date, revision);
        db.Orders.Remove(order);
        await PersistAsync();
        await transaction.CommitAsync();
    }

    // All writers acquire the same database row lock, including completion. This also
    // serializes inserts, which an order's own concurrency token cannot protect.
    private async Task LockRestaurantAsync(int restaurantId) =>
        await db.Restaurants.Where(r => r.Id == restaurantId).ExecuteUpdateAsync(s => s.SetProperty(r => r.Name, r => r.Name));

    private async Task CheckCompletedAsync(int restaurantId, DateOnly date)
    {
        if (await db.CompletedOrderDays.AnyAsync(d => d.RestaurantId == restaurantId && d.Date == date))
            throw new OrderException(409, "Dagens beställning är hämtad och sparad i historiken. Uppdatera listan.");
    }

    public async Task<DailyOrderList> SetCollectorAsync(int restaurantId, int id, CollectorInput input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockRestaurantAsync(restaurantId);
        await RestaurantAsync(restaurantId);
        var (date, _) = CurrentDay();
        if (input.Date != date) throw new OrderException(409, "Dagen har ändrats. Uppdatera listan.");
        await CheckCompletedAsync(restaurantId, date);
        var order = await EditableAsync(restaurantId, id, date, input.Revision);
        if (string.IsNullOrWhiteSpace(order.Name)) throw new OrderException(400, "Ange ett namn i beställningen för att kunna hämta.");
        var orders = await db.Orders.Where(o => o.RestaurantId == restaurantId && o.OrderDate == date).ToListAsync();
        foreach (var row in orders.Where(o => o.OwnerUserId == order.OwnerUserId
            && string.Equals(o.Name, order.Name, StringComparison.OrdinalIgnoreCase)))
        {
            row.CanCollect = input.CanCollect;
            row.Revision = Guid.NewGuid();
        }
        await PersistAsync();
        await transaction.CommitAsync();
        return await GetTodayAsync(restaurantId);
    }

    public async Task<DailyOrderList> CompleteAsync(int restaurantId, CompleteDayInput input)
    {
        if (!user.IsAdmin) throw new OrderException(403, "Endast administratörer får avsluta dagens beställning.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        await LockRestaurantAsync(restaurantId);
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, _) = CurrentDay();
        if (input.Date != date) throw new OrderException(409, "Dagen har ändrats. Uppdatera listan.");
        // Retrying after a lost response must not create a second history entry.
        if (!await db.CompletedOrderDays.AnyAsync(d => d.RestaurantId == restaurantId && d.Date == date))
        {
            var orders = await db.Orders.AsNoTracking().Where(o => o.RestaurantId == restaurantId && o.OrderDate == date).ToListAsync();
            if (orders.Count == 0) throw new OrderException(400, "Det finns inga beställningar att avsluta.");
            if (input.Revisions == null || orders.Count != input.Revisions.Count || orders.Any(o => !input.Revisions.TryGetValue(o.Id, out var revision) || revision != o.Revision))
                throw new OrderException(409, "Listan har ändrats. Uppdatera och kontrollera hämtarna innan du avslutar.");
            var collectors = orders.Where(o => o.CanCollect && !string.IsNullOrWhiteSpace(o.Name))
                .Select(o => o.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
            if (collectors.Count == 0) throw new OrderException(400, "Markera minst en person som hämtat beställningen.");
            db.CompletedOrderDays.Add(new()
            {
                RestaurantId = restaurantId, Date = date, RestaurantName = restaurant.Name,
                IsPizzeria = restaurant.IsPizzeria, CollectedAt = clock.GetUtcNow().UtcDateTime,
                CollectorsJson = JsonSerializer.Serialize(collectors),
                OrdersJson = JsonSerializer.Serialize(orders.Select(Details).ToList())
            });
            await PersistAsync();
        }
        await transaction.CommitAsync();
        return await GetTodayAsync(restaurantId);
    }

    private async Task<PizzaOrder> EditableAsync(int restaurantId, int id, DateOnly date, Guid revision)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.RestaurantId == restaurantId && o.OrderDate == date)
            ?? throw new OrderException(404, "Beställningen finns inte i dagens lista. Uppdatera listan.");
        if (!user.IsAdmin && order.OwnerUserId != user.Id)
            throw new OrderException(403, "Du får bara ändra dina egna beställningar.");
        if (order.Revision != revision)
            throw new OrderException(409, "Någon har ändrat beställningen. Uppdatera listan innan du försöker igen.");
        return order;
    }

    private async Task PersistAsync()
    {
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        { throw new OrderException(409, "Någon har ändrat beställningen. Uppdatera listan innan du försöker igen."); }
    }

    private static OrderDetails Details(PizzaOrder o) => new()
    {
        Id = o.Id, RestaurantId = o.RestaurantId!.Value, MenuItemId = o.MenuItemId!.Value,
        OwnerUserId = o.OwnerUserId,
        UnitPrice = o.UnitPrice, CanCollect = o.CanCollect,
        OrderDate = o.OrderDate!.Value, Pizza = o.Pizza, Name = o.Name, Comment = o.Comment,
        Quantity = o.Quantity, Sauce = o.Sauce, Drink = o.Drink, CreatedAt = o.CreatedAt, Revision = o.Revision
    };
}
