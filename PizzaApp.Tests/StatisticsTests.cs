using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class StatisticsTests
{
    [Fact]
    public async Task Mixed_food_and_standalone_drinks_count_separate_prices_and_all_cans()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.CompletedOrderDays.Add(new()
        {
            RestaurantId = 1, IsPizzeria = true, Date = new(2026, 10, 2),
            OrdersJson = JsonSerializer.Serialize(new OrderDetails[]
            {
                new() { Pizza = "Sallad", Quantity = 2, Drink = "Fanta 33cl" },
                new() { Pizza = "Enbart dryck", DrinkOnly = true, Quantity = 3, Drink = "Fanta 33cl", UnitPrice = 15 }
            })
        });
        await db.SaveChangesAsync();
        var result = await new StatisticsService(db, Options.Create(new StatisticsOptions())).GetAsync();
        Assert.Equal(2, result.TotalMeals);
        Assert.Equal(3, result.ExtraDrinks);
        Assert.Equal(5, result.TotalDrinks);
        Assert.Equal(235m, result.TotalOrderValue);
        Assert.Equal(45m, result.ExtraDrinkValue);
        Assert.Equal(new RankedCount("Sallad", 2), Assert.Single(result.TopPizzas));
        Assert.Equal(new RankedCount("Fanta 33cl", 5), Assert.Single(result.Drinks));
    }

    [Fact]
    public async Task Real_collections_survive_days_whose_orders_have_test_drinks()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        foreach (var day in new[] { 25, 26, 27 })
            db.CompletedOrderDays.Add(new()
            {
                RestaurantId = 1, IsPizzeria = true, Date = new(2026, 9, day),
                CollectorsJson = "[\"Anna\",\"tff\"]",
                OrdersJson = JsonSerializer.Serialize(new[] { new OrderDetails
                { Name = "Anna", Pizza = "Vesuvio", Quantity = 1, Drink = day == 25 ? "Fanta 33 cl" : "Fanta 33cl" } })
            });
        await db.SaveChangesAsync();
        var service = new StatisticsService(db, Options.Create(new StatisticsOptions { StartDate = new(2026, 9, 25) }));
        var result = await service.GetAsync(2026);
        Assert.Equal(new RankedCount("Anna", 3), Assert.Single(result.Collectors));
        Assert.Equal(2, result.TotalMeals);
        Assert.Empty((await service.GetAsync(2025)).Collectors);
    }

    [Fact]
    public async Task Test_rows_dates_and_period_are_excluded_from_all_statistics()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        void AddDay(DateOnly date, string collectors, params OrderDetails[] orders) => db.CompletedOrderDays.Add(new()
        { RestaurantId = 1, IsPizzeria = true, Date = date, CollectorsJson = collectors, OrdersJson = JsonSerializer.Serialize(orders) });
        AddDay(new(2025, 1, 1), "[\"Old\"]", new OrderDetails { Name = "Old", Pizza = "Pizza", Quantity = 100, Drink = "Fanta 33cl" });
        AddDay(new(2026, 1, 1), "[\"Anna\",\"tff\"]",
            new() { Name = "Anna", Pizza = "Sallad", Quantity = 2, Drink = "Fanta 33cl" },
            new() { Name = "Anna", Pizza = "Test", Quantity = 99, Drink = "Fanta 33 cl" },
            new() { Name = "Test", Pizza = "Test", Quantity = 99, Drink = "Sprite 33  cl" },
            new() { Name = " TFF ", Pizza = "Test", Quantity = 99, Drink = "Fanta 33cl" });
        AddDay(new(2026, 1, 2), "[\"tff\"]", new OrderDetails { Name = "tff", Pizza = "Test", Quantity = 99, Drink = "Fanta 33cl" });
        AddDay(new(2026, 1, 3), "[\"Test\"]", new OrderDetails { Name = "Test", Pizza = "Test", Quantity = 99, Drink = "Fanta 33cl" });
        await db.SaveChangesAsync();
        var result = await new StatisticsService(db, Options.Create(new StatisticsOptions
        { StartDate = new(2026, 1, 1), ExcludedDates = [new(2026, 1, 3)] })).GetAsync();
        Assert.Equal(2, result.TotalMeals);
        Assert.Equal(2, result.TotalPizzas);
        Assert.Equal(190m, result.TotalOrderValue);
        Assert.Equal(2, result.TotalDrinks);
        Assert.Equal(new RankedCount("Fanta 33cl", 2), Assert.Single(result.Drinks));
        Assert.Equal(new RankedCount("Anna", 1), Assert.Single(result.Collectors));
        Assert.Equal(new RankedCount("Sallad", 2), Assert.Single(result.TopPizzas));
        Assert.Equal(1, result.CompletedDays);
        Assert.Equal(new[] { 2026 }, result.AvailableYears);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Since);
    }

    [Fact]
    public async Task History_counts_quantities_lunch_meals_drinks_and_unique_collectors_per_day()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        void AddDay(int restaurant, int year, int day, params OrderDetails[] orders) => db.CompletedOrderDays.Add(new()
        {
            RestaurantId = restaurant, IsPizzeria = restaurant == 1, Date = new(year, 1, day),
            OrdersJson = JsonSerializer.Serialize(orders), CollectorsJson = "[\"Anna\",\"anna\",\"Bob\"]"
        });
        AddDay(1, 2025, 1, new OrderDetails { MenuItemId = 2, Pizza = "Vesuvio", Quantity = 3, Drink = "Coca-Cola 33cl", UnitPrice = null });
        AddDay(1, 2026, 1,
            new() { MenuItemId = 2, Pizza = "Vesuvio", Quantity = 2, Drink = "Coca-Cola 33cl", UnitPrice = 85 },
            new() { Category = "Sallader", Pizza = "Sallad", Quantity = 4, Drink = "Fanta 33cl" },
            new() { Category = "Kebab", Pizza = "Kebab", Quantity = 1, Drink = "Coca-Cola 33cl" },
            // Explicit snapshot category wins over a changed menu category.
            new() { MenuItemId = 2, Category = "Sallader", Pizza = "Annan rätt", Quantity = 1 });
        AddDay(2, 2026, 1, new OrderDetails { Category = "Pizzor", Pizza = "Sperring", Quantity = 99, Drink = "Coca-Cola 33cl" });
        // Unfinished orders must not count.
        db.Orders.Add(new() { RestaurantId = 1, MenuItemId = 2, OrderDate = new(2026, 1, 2), Quantity = 99 });
        await db.SaveChangesAsync();
        var service = new StatisticsService(db, Options.Create(new StatisticsOptions()));
        var all = await service.GetAsync();
        Assert.Equal(11, all.TotalPizzas);
        Assert.Equal(11, all.TotalMeals);
        Assert.Equal(1045m, all.TotalOrderValue);
        Assert.Equal(10, all.TotalDrinks);
        Assert.Equal(new[] { 2026, 2025 }, all.AvailableYears);
        Assert.Equal(new DateOnly(2025, 1, 1), all.Since);
        Assert.Equal(new RankedCount("Vesuvio", 5), all.TopPizzas[0]);
        Assert.Contains(new RankedCount("Sallad", 4), all.TopPizzas);
        Assert.Contains(new RankedCount("Kebab", 1), all.TopPizzas);
        Assert.Equal(new RankedCount("Coca-Cola 33cl", 6), all.Drinks[0]);
        Assert.All(all.Collectors, c => Assert.Equal(2, c.Count));
        var yearly = await service.GetAsync(2026);
        Assert.Equal(8, yearly.TotalPizzas);
        Assert.Equal(760m, yearly.TotalOrderValue);
        Assert.Equal(1, yearly.CompletedDays);
        var empty = await service.GetAsync(2024);
        Assert.Equal(0, empty.TotalOrderValue);
        Assert.Empty(empty.TopPizzas);
        Assert.Empty(empty.Collectors);
    }

    [Fact]
    public async Task Pizza_top_five_is_ranked_by_portions_and_breaks_ties_consistently()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.CompletedOrderDays.Add(new CompletedOrderDay { RestaurantId = 1, IsPizzeria = true, Date = new(2026, 1, 1),
            OrdersJson = JsonSerializer.Serialize(Enumerable.Range(1, 7).Select(i =>
                new OrderDetails { Category = "Pizzor", Pizza = $"Pizza {i}", Quantity = i })) });
        await db.SaveChangesAsync();
        var result = await new StatisticsService(db, Options.Create(new StatisticsOptions())).GetAsync();
        Assert.Equal(new[] { 7, 6, 5, 4, 3 }, result.TopPizzas.Select(p => p.Count));
        Assert.Equal(28, result.TotalPizzas);
    }
}
