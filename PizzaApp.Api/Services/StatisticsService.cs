using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Shared;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace PizzaApp.Api.Services;

public class StatisticsService(PizzaDbContext db, IOptions<StatisticsOptions> options)
{
    public async Task<PizzeriaStatistics> GetAsync(int? year = null)
    {
        var settings = options.Value;
        var excludedNames = settings.ExcludedNames.Select(n => n.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var history = await db.CompletedOrderDays.AsNoTracking().Where(d => d.IsPizzeria
                && (!settings.StartDate.HasValue || d.Date >= settings.StartDate.Value))
            .OrderBy(d => d.Date).ToListAsync();
        // Known test rows are omitted from every total, not just their drink ranking.
        var eligibleHistory = history.Where(d => !settings.ExcludedDates.Contains(d.Date)).ToList();
        var cleanHistory = eligibleHistory
            .Select(d => new { Day = d, Orders = (JsonSerializer.Deserialize<List<OrderDetails>>(d.OrdersJson) ?? [])
                .Where(o => !excludedNames.Contains(o.Name.Trim())
                    && !Regex.IsMatch(o.Drink ?? "", @"33\s+cl\b", RegexOptions.IgnoreCase)).ToList() })
            .Where(d => d.Orders.Count > 0).ToList();
        var days = cleanHistory.Where(d => !year.HasValue || d.Day.Date.Year == year).ToList();
        var orders = days.SelectMany(d => d.Orders).ToList();
        var drinks = orders.Where(o => PizzaChoices.Drinks.Contains(o.Drink, StringComparer.Ordinal)).ToList();
        return new()
        {
            Year = year,
            StartDate = settings.StartDate,
            AvailableYears = cleanHistory.Select(d => d.Day.Date.Year).Distinct().OrderDescending().ToList(),
            Since = cleanHistory.Count == 0 ? null : cleanHistory[0].Day.Date,
            TotalPizzas = orders.Sum(o => o.Quantity),
            TotalMeals = orders.Sum(o => o.Quantity),
            TotalDrinks = drinks.Sum(o => o.Quantity),
            CompletedDays = days.Count,
            TopPizzas = Rank(orders.Select(o => new RankedCount(o.Pizza, o.Quantity))).Take(5).ToList(),
            Drinks = Rank(drinks
                .Select(o => new RankedCount(o.Drink!, o.Quantity))),
            // Collectors are an independent record of who fetched the food.
            // A filtered order or obsolete drink must not erase a real collection.
            Collectors = Rank(eligibleHistory.Where(d => !year.HasValue || d.Date.Year == year)
                .SelectMany(d => (JsonSerializer.Deserialize<List<string>>(d.CollectorsJson) ?? [])
                .Where(n => !string.IsNullOrWhiteSpace(n) && !excludedNames.Contains(n.Trim()))
                .Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(n => new RankedCount(n, 1))))
        };
    }

    private static List<RankedCount> Rank(IEnumerable<RankedCount> rows) => rows
        .GroupBy(r => r.Name.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(g => new RankedCount(g.Key, g.Sum(r => r.Count)))
        .OrderByDescending(r => r.Count).ThenBy(r => r.Name, StringComparer.Ordinal).ToList();
}
