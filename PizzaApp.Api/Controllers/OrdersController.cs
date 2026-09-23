using Microsoft.AspNetCore.Mvc;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:int}/orders")]
public class OrdersController(OrderService service) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<DailyOrderList>> GetToday(int restaurantId) =>
        Handle(() => service.GetTodayAsync(restaurantId));

    [HttpPost]
    public Task<ActionResult<OrderDetails>> Create(int restaurantId, OrderInput input) =>
        Handle(() => service.SaveAsync(restaurantId, input));

    [HttpPut("{id:int}/collector")]
    public Task<ActionResult<DailyOrderList>> SetCollector(int restaurantId, int id, CollectorInput input) =>
        Handle(() => service.SetCollectorAsync(restaurantId, id, input));

    [HttpPost("complete")]
    public Task<ActionResult<DailyOrderList>> Complete(int restaurantId, CompleteDayInput input) =>
        Handle(() => service.CompleteAsync(restaurantId, input));

    [HttpPut("{id:int}")]
    public Task<ActionResult<OrderDetails>> Update(int restaurantId, int id, OrderInput input) =>
        Handle(() => service.SaveAsync(restaurantId, input, id));

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
