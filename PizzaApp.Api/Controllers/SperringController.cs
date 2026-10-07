using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/sperring")]
public class SperringController(PizzaDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SperringPage>> Get()
    {
        var restaurant = await db.Restaurants.SingleAsync(r => r.Name == "Sperring");
        var rows = await db.SperringWeeks.AsNoTracking().OrderBy(w => w.Id).ToListAsync();
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.GetUtcNow(), "Europe/Stockholm").DateTime;
        var menus = Enumerable.Range(1, 2).Select(slot =>
        {
            var row = rows.SingleOrDefault(w => w.Id == slot);
            if (row != null) return JsonSerializer.Deserialize<SperringMenu>(row.MenuJson)!;
            var date = today.AddDays((slot - 1) * 7);
            return new SperringMenu { Slot = slot, Year = ISOWeek.GetYear(date), Week = ISOWeek.GetWeekOfYear(date) };
        }).ToList();
        var dishes = await db.MenuItems.Where(m => m.RestaurantId == restaurant.Id && !m.IsHidden)
            .Select(m => new SperringDish(m.Id, m.Name)).ToListAsync();
        return new SperringPage(restaurant.Id, menus, dishes);
    }

    [HttpPut("{slot:int}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Save(int slot, SperringMenu input)
    {
        if (slot is < 1 or > 2 || input.Slot != slot || input.Year is < 2000 or > 2100
            || input.Week < 1 || input.Week > ISOWeek.GetWeeksInYear(input.Year)
            || input.RegularDishes == null || input.Days == null || input.Days.Length != 5
            || input.Days.Any(d => d == null))
            return Problem(statusCode: 400, detail: "Ange ett giltigt år, veckonummer och fem vardagar.");
        var texts = input.Days.Append(input.RegularDishes).ToArray();
        if (texts.Any(t => t.Length > 6000 || SperringMenu.Lines(t).Length > 30 || SperringMenu.Lines(t).Any(n => n.Length > 200)))
            return Problem(statusCode: 400, detail: "Ange högst 30 rätter per fält och 200 tecken per rätt.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var restaurant = await db.Restaurants.SingleAsync(r => r.Name == "Sperring");
        // Share the order writer's lock so publishing and ordering cannot race.
        await db.Restaurants.Where(r => r.Id == restaurant.Id).ExecuteUpdateAsync(s => s.SetProperty(r => r.Name, r => r.Name));
        var rows = await db.SperringWeeks.ToListAsync();
        var row = rows.SingleOrDefault(w => w.Id == slot);
        if ((row?.Revision ?? Guid.Empty) != input.Revision)
            return Problem(statusCode: 409, detail: "Menyn har ändrats av en annan admin. Ladda om innan du sparar.");
        if (rows.Where(w => w.Id != slot).Select(w => JsonSerializer.Deserialize<SperringMenu>(w.MenuJson)!)
            .Any(m => m.Year == input.Year && m.Week == input.Week))
            return Problem(statusCode: 400, detail: "Välj två olika veckor.");
        var items = await db.MenuItems.Where(m => m.RestaurantId == restaurant.Id).ToListAsync();
        foreach (var name in texts.SelectMany(SperringMenu.Lines).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var item = items.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (item == null) db.MenuItems.Add(new MenuItem { RestaurantId = restaurant.Id, Name = name });
            else item.IsHidden = false;
        }
        input.Revision = Guid.NewGuid();
        if (row == null) { row = new SperringWeek { Id = slot }; db.SperringWeeks.Add(row); }
        row.Revision = input.Revision;
        row.MenuJson = JsonSerializer.Serialize(input);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }
}
