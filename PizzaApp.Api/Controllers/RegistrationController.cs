using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("Registration")]
[RequestSizeLimit(8192)]
[Route("api/registration")]
public class RegistrationController(RegistrationService service) : ControllerBase
{
    [HttpPost("start")]
    public Task<IActionResult> Start(RegistrationInput input, CancellationToken ct) => Handle(() => service.StartAsync(input, ct));
    [HttpPost("activate")]
    public Task<IActionResult> Activate(RegistrationInput input, CancellationToken ct) => Handle(() => service.ActivateAsync(input, ct));

    private async Task<IActionResult> Handle(Func<Task> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { await action(); return NoContent(); }
        catch (OrderException ex) { return Problem(statusCode: ex.StatusCode, detail: ex.Message); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException)
        { return Problem(statusCode: 503, detail: "Kunde inte läsa registreringstjänstens svar. Om kontot redan skapats, välj Slutför befintligt konto."); }
    }
}
