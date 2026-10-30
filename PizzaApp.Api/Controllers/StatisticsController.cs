// Statistik-API: tillgängliga år och beställningshistorik.

using Microsoft.AspNetCore.Mvc;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/statistics/pizzeria")]
public class StatisticsController(StatisticsService service) : ControllerBase
{
    // LÄSNING: hämta data för klienten.
    [HttpGet]
    public async Task<ActionResult<PizzeriaStatistics>> Get([FromQuery] int? year)
    {
        if (year is < 1 or > 9999) return BadRequest("Ange ett giltigt år.");
        return Ok(await service.GetAsync(year));
    }
}
