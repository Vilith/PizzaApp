// Beställnings-API: dagslistor, rätter, hämtare och avslutade dagar.

using Microsoft.AspNetCore.Mvc;
using PizzaApp.Api.Services;
using PizzaApp.Shared;
using Microsoft.AspNetCore.Authorization;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:int}/orders")]
public class OrdersController(OrderService service) : ControllerBase
{
    // LÄSNING: hämta data för klienten.
    [HttpGet]
    public Task<ActionResult<DailyOrderList>> GetToday(int restaurantId, [FromQuery] DateOnly? date) =>
        Handle(() => service.GetDayAsync(restaurantId, date));

    // ÅTGÄRD: skapa data eller utför kommandot efter validering.
    [HttpPost]
    public Task<ActionResult<OrderDetails>> Create(int restaurantId, OrderInput input) =>
        Handle(() => service.SaveAsync(restaurantId, input));

    // ÄNDRING: validera och spara uppgifterna.
    [HttpPut("{id:int}/collector")]
    public Task<ActionResult<DailyOrderList>> SetCollector(int restaurantId, int id, CollectorInput input) =>
        Handle(() => service.SetCollectorAsync(restaurantId, id, input));

    // ÅTGÄRD: skapa data eller utför kommandot efter validering.
    [HttpPost("complete")]
    [Authorize(Policy = "Admin")]
    public Task<ActionResult<DailyOrderList>> Complete(int restaurantId, CompleteDayInput input) =>
        Handle(() => service.CompleteAsync(restaurantId, input));

    // ÄNDRING: validera och spara uppgifterna.
    [HttpPut("{id:int}")]
    public Task<ActionResult<OrderDetails>> Update(int restaurantId, int id, OrderInput input) =>
        Handle(() => service.SaveAsync(restaurantId, input, id));

    // BORTTAGNING: kontrollera behörighet och ta bort posten.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int restaurantId, int id, [FromQuery] Guid revision)
    {
        try { await service.DeleteAsync(restaurantId, id, revision); return NoContent(); }
        catch (OrderException ex) { return Problem(detail: ex.Message, statusCode: ex.StatusCode); }
    }

    private async Task<ActionResult<T>> Handle<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (OrderException ex) { return Problem(detail: ex.Message, statusCode: ex.StatusCode); }
    }
}
