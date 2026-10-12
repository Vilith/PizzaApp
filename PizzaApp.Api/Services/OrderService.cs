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

    public Task<DailyOrderList> GetTodayAsync(int restaurantId) => GetDayAsync(restaurantId);

    public async Task<DailyOrderList> GetDayAsync(int restaurantId, DateOnly? requested = null)
    {
        var restaurant = await RestaurantAsync(restaurantId);
        var (today, passed) = CurrentDay();
        var date = requested ?? today;
        if (date != today && restaurant.Name != "Sperring")
            throw new OrderException(400, "Endast Sperring har beställningar per datum.");
        var completed = await db.CompletedOrderDays.AsNoTracking().SingleOrDefaultAsync(d => d.RestaurantId == restaurantId && d.Date == date);
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId && o.OrderDate == date)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToListAsync();
        var owners = orders.Where(o => o.OwnerUserId.HasValue).Select(o => o.OwnerUserId!.Value).Distinct().ToArray();
        var avatars = await db.UserProfiles.AsNoTracking().Where(p => owners.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.AvatarDataUrl);
        var details = orders.Select(o =>
        {
            var detail = Details(o);
            if (o.OwnerUserId.HasValue) detail.AvatarDataUrl = avatars.GetValueOrDefault(o.OwnerUserId.Value);
            return detail;
        }).ToList();
        return new()
        {
            Date = date, IsPizzeria = restaurant.IsPizzeria,
            CollectedAt = completed?.CollectedAt,
            CollectedBy = completed == null ? [] : JsonSerializer.Deserialize<List<string>>(completed.CollectorsJson)!,
            DeadlinePassed = restaurant.IsPizzeria && passed,
            IsLocked = completed != null || restaurant.IsPizzeria && passed && options.Value.LockAfterDeadline,
            Orders = details,
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
        var (today, passed) = CurrentDay();
        var date = input.DeliveryDate ?? today;
        if (date < today || (date != today && restaurant.Name != "Sperring"))
            throw new OrderException(400, "Välj dagens datum eller en kommande vardag på Sperringmenyn.");
        await CheckCompletedAsync(restaurantId, date);
        CheckDeadline(restaurant, passed);
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, true))
            throw new OrderException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
        if (input.DrinkOnly && (!restaurant.IsPizzeria || input.MenuItemId != 0 || input.Sauce != null))
            throw new OrderException(400, "Enbart dryck kan beställas från pizzerian utan maträtt eller sås.");
        if (input.DrinkOnly && !PizzaChoices.Drinks.Contains(input.Drink))
            throw new OrderException(400, "Välj en dryck.");
        if (restaurant.IsPizzeria && ((!input.DrinkOnly && !PizzaChoices.Sauces.Contains(input.Sauce)) || !PizzaChoices.Drinks.Contains(input.Drink)))
            throw new OrderException(400, "Välj en sås (eller Ingen sås) och en dryck.");
        if (!restaurant.IsPizzeria && (input.Sauce != null || input.Drink != null))
            throw new OrderException(400, "À la carte har inga val för sås eller dryck.");
        var item = input.DrinkOnly ? null : await db.MenuItems.AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == input.MenuItemId && m.RestaurantId == restaurantId);
        if (!input.DrinkOnly && item == null) throw new OrderException(400, "Rätten finns inte på restaurangens meny.");
        var order = id.HasValue ? await EditableAsync(restaurantId, id.Value, date, input.Revision) : new PizzaOrder
        { OwnerUserId = user.Id, RestaurantId = restaurantId, OrderDate = date, CreatedAt = clock.GetUtcNow().UtcDateTime };
        if (restaurant.Name == "Sperring")
        {
            var weeks = await db.SperringWeeks.AsNoTracking().ToListAsync();
            var regular = weeks.SingleOrDefault(w => w.Id == 0);
            var regularDishes = regular == null ? null : JsonSerializer.Deserialize<SperringMenu>(regular.MenuJson)!.RegularDishes;
            var menus = Enumerable.Range(1, 2).Select(slot =>
            {
                var row = weeks.SingleOrDefault(w => w.Id == slot);
                var other = weeks.FirstOrDefault(w => w.Id is 1 or 2);
                var day = other == null ? today.AddDays((slot - 1) * 7).ToDateTime(TimeOnly.MinValue) : JsonSerializer.Deserialize<SperringMenu>(other.MenuJson)!.Monday.AddDays((slot - other.Id) * 7).ToDateTime(TimeOnly.MinValue);
                var menu = row == null ? new SperringMenu { Year = System.Globalization.ISOWeek.GetYear(day), Week = System.Globalization.ISOWeek.GetWeekOfYear(day) } : JsonSerializer.Deserialize<SperringMenu>(row.MenuJson)!;
                menu.RegularDishes = regularDishes ?? (weeks.FirstOrDefault(w => w.Id == 1) is { } first ? JsonSerializer.Deserialize<SperringMenu>(first.MenuJson)!.RegularDishes : "");
                return menu;
            });
            var available = menus
                .Where(w => date >= w.Monday && date <= w.Monday.AddDays(4))
                .Any(w => w.Dishes(date.DayNumber - w.Monday.DayNumber).Contains(item!.Name, StringComparer.OrdinalIgnoreCase));
            if (!available) throw new OrderException(400, "Rätten finns inte på Sperringmenyn för valt datum. Uppdatera sidan.");
        }
        if (item is { IsHidden: true } && (!id.HasValue || order.MenuItemId != item.Id))
            throw new OrderException(400, "Rätten har utgått från menyn.");
        if (input.DrinkOnly) order.UnitPrice = PizzeriaStatistics.ExtraDrinkPrice;
        else if (!id.HasValue || order.MenuItemId != item!.Id) order.UnitPrice = item!.Price;
        if (!id.HasValue)
        {
            var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null || string.IsNullOrWhiteSpace(profile.DisplayName))
                throw new OrderException(409, "Välj ditt namn eller nick under Inställningar innan du beställer.");
            order.Name = profile.DisplayName;
        }
        order.MenuItemId = item?.Id;
        order.Pizza = input.DrinkOnly ? "Enbart dryck" : item!.Name;
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
        var (today, passed) = CurrentDay();
        var date = restaurant.Name == "Sperring"
            ? await db.Orders.Where(o => o.Id == id && o.RestaurantId == restaurantId).Select(o => o.OrderDate).SingleOrDefaultAsync() ?? today
            : today;
        if (date < today) throw new OrderException(409, "Tidigare dagars beställningar kan inte ändras.");
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
            var categories = await db.MenuItems.AsNoTracking().Where(m => m.RestaurantId == restaurantId)
                .ToDictionaryAsync(m => m.Id, m => m.Category);
            var snapshot = orders.Select(o =>
            {
                var details = Details(o);
                details.Category = details.DrinkOnly ? "Dryck" : categories.GetValueOrDefault(details.MenuItemId);
                return details;
            }).ToList();
            db.CompletedOrderDays.Add(new()
            {
                RestaurantId = restaurantId, Date = date, RestaurantName = restaurant.Name,
                IsPizzeria = restaurant.IsPizzeria, CollectedAt = clock.GetUtcNow().UtcDateTime,
                CollectorsJson = JsonSerializer.Serialize(collectors),
                OrdersJson = JsonSerializer.Serialize(snapshot)
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
        Id = o.Id, RestaurantId = o.RestaurantId!.Value, MenuItemId = o.MenuItemId ?? 0,
        DrinkOnly = o.MenuItemId == null && o.Drink != null,
        OwnerUserId = o.OwnerUserId,
        UnitPrice = o.UnitPrice, CanCollect = o.CanCollect,
        OrderDate = o.OrderDate!.Value, Pizza = o.Pizza, Name = o.Name, Comment = o.Comment,
        Quantity = o.Quantity, Sauce = o.Sauce, Drink = o.Drink, CreatedAt = o.CreatedAt, Revision = o.Revision
    };
}
