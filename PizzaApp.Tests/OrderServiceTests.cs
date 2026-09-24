using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class OrderServiceTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly PizzaDbContext db;
    private readonly TestClock clock = new();
    private readonly OrderingOptions settings = new();
    private readonly OrderService service;
    private readonly TestUser actor = new();

    public OrderServiceTests()
    {
        connection.Open();
        db = new(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        db.MenuItems.Add(new MenuItem { Id = 100, Name = "Dagens rätt", Price = 100, RestaurantId = 2 });
        db.UserProfiles.Add(new() { UserId = actor.Id, DisplayName = "Anna", Revision = Guid.NewGuid() });
        db.SaveChanges();
        service = new(db, clock, Options.Create(settings), actor);
    }

    private static OrderInput Pizza(int quantity = 1, string sauce = "Vitlökssås") => new()
    { MenuItemId = 2, Quantity = quantity, Sauce = sauce, Drink = "Vatten" };

    [Fact]
    public async Task Collector_choice_does_not_change_another_account_with_the_same_name()
    {
        actor.IsAdmin = false;
        var input = Pizza(); input.Name = "Anna";
        var first = await service.SaveAsync(1, input);
        actor.Id = Guid.NewGuid();
        db.UserProfiles.Add(new() { UserId = actor.Id, DisplayName = "Anna", Revision = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var second = await service.SaveAsync(1, input);
        var day = await service.SetCollectorAsync(1, second.Id, new(true, second.Revision, second.OrderDate));
        Assert.True(day.Orders.Single(o => o.Id == second.Id).CanCollect);
        Assert.False(day.Orders.Single(o => o.Id == first.Id).CanCollect);
        Assert.Equal(403, (await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1,
            new(day.Date, day.Orders.ToDictionary(o => o.Id, o => o.Revision))))).StatusCode);
    }

    [Fact]
    public async Task Legacy_orders_without_owner_can_only_be_edited_by_admin()
    {
        var order = await service.SaveAsync(1, Pizza());
        (await db.Orders.SingleAsync()).OwnerUserId = null;
        await db.SaveChangesAsync();
        actor.IsAdmin = false;
        Assert.Equal(403, (await Assert.ThrowsAsync<OrderException>(() => service.DeleteAsync(1, order.Id, order.Revision))).StatusCode);
        actor.IsAdmin = true;
        await service.DeleteAsync(1, order.Id, order.Revision);
    }

    [Fact]
    public async Task Wireframe_ala_carte_menu_can_be_ordered_without_pizza_choices()
    {
        var item = await db.MenuItems.SingleAsync(m => m.RestaurantId == 2 && m.Name == "Oxfilé med potatis");
        var saved = await service.SaveAsync(2, new() { MenuItemId = item.Id, Quantity = 2 });
        Assert.Equal("Oxfilé med potatis", saved.Pizza);
        Assert.Equal(2, Assert.Single((await service.GetTodayAsync(2)).Summary).Quantity);
        Assert.Null(saved.Sauce);
    }

    [Fact]
    public async Task Uses_profile_name_menu_name_and_swedish_date()
    {
        clock.Now = DateTimeOffset.Parse("2026-09-21T22:30:00Z");
        var saved = await service.SaveAsync(1, Pizza(2));
        Assert.Equal("Vesuvio", saved.Pizza);
        Assert.Equal(new DateOnly(2026, 9, 22), saved.OrderDate);
        Assert.Equal("Anna", saved.Name);
        Assert.Equal(2, saved.Quantity);
        Assert.NotEqual(Guid.Empty, saved.Revision);
    }

    [Fact]
    public async Task Today_is_separate_per_restaurant_and_does_not_include_yesterday()
    {
        clock.Now = DateTimeOffset.Parse("2026-09-21T09:00:00Z");
        await service.SaveAsync(1, Pizza());
        clock.Now = DateTimeOffset.Parse("2026-09-22T09:00:00Z");
        await service.SaveAsync(1, Pizza(3));
        await service.SaveAsync(2, new() { MenuItemId = 100 });
        var pizzas = await service.GetTodayAsync(1);
        Assert.Equal(3, Assert.Single(pizzas.Orders).Quantity);
        Assert.Equal("Dagens rätt", Assert.Single((await service.GetTodayAsync(2)).Orders).Pizza);
    }

    [Fact]
    public async Task Summary_combines_identical_choices_but_preserves_sauces_drinks_and_comments()
    {
        await service.SaveAsync(1, Pizza(2));
        await service.SaveAsync(1, Pizza(3));
        await service.SaveAsync(1, Pizza(1, "Ingen sås"));
        var special = Pizza(); special.Comment = "Utan ost";
        await service.SaveAsync(1, special);
        var drink = Pizza(); drink.Drink = "Fanta 33 cl";
        await service.SaveAsync(1, drink);
        var summary = (await service.GetTodayAsync(1)).Summary;
        Assert.Equal(4, summary.Count);
        Assert.Contains(summary, s => s.Quantity == 5 && s.Sauce == "Vitlökssås" && s.Drink == "Vatten");
        Assert.Contains(summary, s => s.Comment == "Utan ost" && s.Quantity == 1);
    }

    [Theory]
    [InlineData("2026-09-22T09:14:59Z", false)]
    [InlineData("2026-09-22T09:15:00Z", true)]
    [InlineData("2026-01-22T10:14:59Z", false)]
    [InlineData("2026-01-22T10:15:00Z", true)]
    public async Task Deadline_is_1115_in_Stockholm_in_summer_and_winter(string now, bool passed)
    {
        clock.Now = DateTimeOffset.Parse(now);
        Assert.Equal(passed, (await service.GetTodayAsync(1)).DeadlinePassed);
    }

    [Fact]
    public async Task Late_create_update_and_delete_are_allowed_by_default()
    {
        clock.Now = DateTimeOffset.Parse("2026-09-22T12:00:00Z");
        var saved = await service.SaveAsync(1, Pizza());
        var edit = Pizza(4); edit.Revision = saved.Revision;
        var updated = await service.SaveAsync(1, edit, saved.Id);
        Assert.Equal(4, updated.Quantity);
        Assert.True((await service.GetTodayAsync(1)).DeadlinePassed);
        Assert.False((await service.GetTodayAsync(1)).IsLocked);
        await service.DeleteAsync(1, saved.Id, updated.Revision);
        Assert.Empty((await service.GetTodayAsync(1)).Orders);
    }

    [Fact]
    public async Task Enabling_lock_blocks_all_pizza_mutations_at_deadline_but_not_other_restaurant()
    {
        var saved = await service.SaveAsync(1, Pizza());
        settings.LockAfterDeadline = true;
        clock.Now = DateTimeOffset.Parse("2026-09-22T09:15:00Z");
        var edit = Pizza(4); edit.Revision = saved.Revision;
        Assert.Equal(409, (await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, Pizza()))).StatusCode);
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, edit, saved.Id));
        await Assert.ThrowsAsync<OrderException>(() => service.DeleteAsync(1, saved.Id, saved.Revision));
        Assert.True((await service.GetTodayAsync(1)).IsLocked);
        await service.SaveAsync(2, new() { MenuItemId = 100 });
    }

    [Fact]
    public async Task Cannot_order_another_restaurants_item_or_edit_another_restaurants_order()
    {
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(2, Pizza()));
        var saved = await service.SaveAsync(1, Pizza());
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(2, new() { MenuItemId = 100, Revision = saved.Revision }, saved.Id));
        await Assert.ThrowsAsync<OrderException>(() => service.DeleteAsync(2, saved.Id, saved.Revision));
        Assert.Single((await service.GetTodayAsync(1)).Orders);
    }

    [Theory]
    [InlineData(0, "Vitlökssås", "Vatten")]
    [InlineData(100, "Vitlökssås", "Vatten")]
    [InlineData(1, null, "Vatten")]
    [InlineData(1, "Vitlökssås", null)]
    [InlineData(1, "Fel sås", "Vatten")]
    public async Task Invalid_quantity_or_missing_choices_cannot_be_saved(int quantity, string? sauce, string? drink)
    {
        var input = Pizza(quantity); input.Sauce = sauce; input.Drink = drink;
        Assert.Equal(400, (await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, input))).StatusCode);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Ala_carte_does_not_accept_pizza_extras()
    {
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(2, new() { MenuItemId = 100, Sauce = "Vitlökssås" }));
    }

    [Fact]
    public async Task Stale_edits_are_rejected_and_previous_days_orders_cannot_be_changed()
    {
        var saved = await service.SaveAsync(1, Pizza());
        var edit = Pizza(2); edit.Revision = saved.Revision;
        await service.SaveAsync(1, edit, saved.Id);
        Assert.Equal(409, (await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, edit, saved.Id))).StatusCode);
        clock.Now = clock.Now.AddDays(1);
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, edit, saved.Id));
    }

    public void Dispose() { db.Dispose(); connection.Dispose(); }
    [Fact]
    public async Task Completed_day_preserves_prices_collectors_and_choices_and_is_immutable()
    {
        var input = Pizza(3); input.Name = "Anna";
        var order = await service.SaveAsync(1, input);
        var day = await service.SetCollectorAsync(1, order.Id, new(true, order.Revision, order.OrderDate));
        var request = new CompleteDayInput(day.Date, day.Orders.ToDictionary(o => o.Id, o => o.Revision));
        var completed = await service.CompleteAsync(1, request);
        Assert.True(completed.IsLocked);
        Assert.Equal("Anna", Assert.Single(completed.CollectedBy));
        await service.CompleteAsync(1, request);
        Assert.Single(db.CompletedOrderDays);
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<List<OrderDetails>>((await db.CompletedOrderDays.SingleAsync()).OrdersJson)!;
        Assert.Equal(95m, Assert.Single(snapshot).UnitPrice);
        Assert.Equal(3, snapshot[0].Quantity);
        Assert.Equal(input.Sauce, snapshot[0].Sauce);
        Assert.Equal(input.Drink, snapshot[0].Drink);
        await Assert.ThrowsAsync<OrderException>(() => service.SaveAsync(1, Pizza()));
        await Assert.ThrowsAsync<OrderException>(() => service.DeleteAsync(1, order.Id, day.Orders[0].Revision));
        await Assert.ThrowsAsync<OrderException>(() => service.SetCollectorAsync(1, order.Id, new(false, day.Orders[0].Revision, day.Date)));
        clock.Now = clock.Now.AddDays(1);
        Assert.Empty((await service.GetTodayAsync(1)).Orders);
        Assert.False((await service.GetTodayAsync(1)).IsLocked);
        await service.SaveAsync(1, Pizza());
        Assert.Single(db.CompletedOrderDays);
    }

    [Fact]
    public async Task Completion_rejects_empty_unassigned_stale_and_wrong_day_lists()
    {
        var day = await service.GetTodayAsync(1);
        await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1, new(day.Date, [])));
        var input = Pizza(); input.Name = "Anna";
        var order = await service.SaveAsync(1, input);
        var request = new CompleteDayInput(day.Date, new() { [order.Id] = order.Revision });
        await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1, request));
        day = await service.SetCollectorAsync(1, order.Id, new(true, order.Revision, order.OrderDate));
        await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1, request));
        var revisions = day.Orders.ToDictionary(o => o.Id, o => o.Revision);
        await service.SaveAsync(1, Pizza());
        await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1, new(day.Date, revisions)));
        await Assert.ThrowsAsync<OrderException>(() => service.CompleteAsync(1, new(day.Date.AddDays(-1), revisions)));
        Assert.Empty(db.CompletedOrderDays);
    }

    [Fact]
    public async Task Menu_price_changes_do_not_reprice_existing_orders_or_completed_history()
    {
        var input = Pizza(2); input.Name = "Anna";
        var order = await service.SaveAsync(1, input);
        var menuItem = await db.MenuItems.SingleAsync(m => m.Id == input.MenuItemId);
        menuItem.Price = 120m;
        await db.SaveChangesAsync();
        input.Revision = order.Revision;
        input.Comment = "Utan lök";
        order = await service.SaveAsync(1, input, order.Id);
        Assert.Equal(95m, order.UnitPrice);
        Assert.Equal(120m, (await service.SaveAsync(1, Pizza())).UnitPrice);
        var day = await service.SetCollectorAsync(1, order.Id, new(true, order.Revision, order.OrderDate));
        await service.CompleteAsync(1, new(day.Date, day.Orders.ToDictionary(o => o.Id, o => o.Revision)));
        menuItem.Price = 150m;
        await db.SaveChangesAsync();
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<List<OrderDetails>>((await db.CompletedOrderDays.SingleAsync()).OrdersJson)!;
        Assert.Equal(310m, snapshot.Sum(o => o.UnitPrice * o.Quantity));
    }

    [Fact]
    public async Task Collectors_can_change_after_deadline_and_duplicate_names_count_once()
    {
        var input = Pizza(); input.Name = "Anna";
        var first = await service.SaveAsync(1, input);
        input.Name = "anna";
        await service.SaveAsync(1, input);
        settings.LockAfterDeadline = true;
        clock.Now = clock.Now.AddHours(3);
        var day = await service.SetCollectorAsync(1, first.Id, new(true, first.Revision, first.OrderDate));
        Assert.All(day.Orders, o => Assert.True(o.CanCollect));
        var completed = await service.CompleteAsync(1, new(day.Date, day.Orders.ToDictionary(o => o.Id, o => o.Revision)));
        Assert.Single(completed.CollectedBy);
        Assert.False((await service.GetTodayAsync(2)).IsLocked);
    }
    [Fact]
    public async Task Concurrent_database_update_does_not_overwrite_another_users_changes()
    {
        var saved = await service.SaveAsync(1, Pizza());
        using var otherDb = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await otherDb.Orders.SingleAsync(o => o.Id == saved.Id);
        var otherService = new OrderService(otherDb, clock, Options.Create(settings), actor);
        var edit = Pizza(2); edit.Revision = saved.Revision;
        await service.SaveAsync(1, edit, saved.Id);
        Assert.Equal(409, (await Assert.ThrowsAsync<OrderException>(() => otherService.SaveAsync(1, edit, saved.Id))).StatusCode);
        Assert.Equal(2, Assert.Single((await service.GetTodayAsync(1)).Orders).Quantity);
    }

    [Fact]
    public async Task Historical_orders_without_restaurant_are_preserved_but_excluded_from_today()
    {
        db.Orders.Add(new() { Name = "Tidigare beställare", Pizza = "Vesuvio", CreatedAt = clock.Now.UtcDateTime });
        await db.SaveChangesAsync();
        Assert.Empty((await service.GetTodayAsync(1)).Orders);
        Assert.Single(db.Orders);
    }

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-09-22T09:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
