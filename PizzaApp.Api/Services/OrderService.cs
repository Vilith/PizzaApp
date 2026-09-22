using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

public class OrderService(PizzaDbContext db, TimeProvider clock, IOptions<OrderingOptions> options)
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
        var orders = await db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == restaurantId && o.OrderDate == date)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToListAsync();
        return new()
        {
            Date = date, IsPizzeria = restaurant.IsPizzeria,
            DeadlinePassed = restaurant.IsPizzeria && passed,
            IsLocked = restaurant.IsPizzeria && passed && options.Value.LockAfterDeadline,
            Orders = orders.Select(Details).ToList(),
            Summary = orders.GroupBy(o => new { o.MenuItemId, o.Pizza, o.Sauce, o.Drink, o.Comment })
                .Select(g => new OrderSummary(g.Key.Pizza, g.Key.Sauce, g.Key.Drink, g.Key.Comment, g.Sum(o => o.Quantity)))
                .OrderBy(o => o.Pizza).ThenBy(o => o.Sauce).ThenBy(o => o.Drink).ThenBy(o => o.Comment).ToList()
        };
    }

    public async Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null)
    {
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, passed) = CurrentDay();
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
        { RestaurantId = restaurantId, OrderDate = date, CreatedAt = clock.GetUtcNow().UtcDateTime };
        order.MenuItemId = item.Id;
        order.Pizza = item.Name;
        order.Name = input.Name?.Trim() ?? "";
        order.Comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim();
        order.Quantity = input.Quantity;
        order.Sauce = input.Sauce;
        order.Drink = input.Drink;
        order.Revision = Guid.NewGuid();
        if (!id.HasValue) db.Orders.Add(order);
        await PersistAsync();
        return Details(order);
    }

    public async Task DeleteAsync(int restaurantId, int id, Guid revision)
    {
        var restaurant = await RestaurantAsync(restaurantId);
        var (date, passed) = CurrentDay();
        CheckDeadline(restaurant, passed);
        var order = await EditableAsync(restaurantId, id, date, revision);
        db.Orders.Remove(order);
        await PersistAsync();
    }

    private async Task<PizzaOrder> EditableAsync(int restaurantId, int id, DateOnly date, Guid revision)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.RestaurantId == restaurantId && o.OrderDate == date)
            ?? throw new OrderException(404, "Beställningen finns inte i dagens lista. Uppdatera listan.");
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
        OrderDate = o.OrderDate!.Value, Pizza = o.Pizza, Name = o.Name, Comment = o.Comment,
        Quantity = o.Quantity, Sauce = o.Sauce, Drink = o.Drink, CreatedAt = o.CreatedAt, Revision = o.Revision
    };
}
