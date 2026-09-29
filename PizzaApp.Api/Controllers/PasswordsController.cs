using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Data;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController, Route("api/passwords"), Authorize]
[EnableRateLimiting("Registration"), RequestSizeLimit(4096)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PasswordsController(PizzaDbContext db, ICurrentUser user,
    IOptions<SupabaseOptions> options, IHttpClientFactory clients) : ControllerBase
{
    [HttpGet("accounts"), Authorize(Policy = "Admin")]
    public async Task<ActionResult<List<PasswordAccount>>> Accounts(CancellationToken ct)
        => await db.UserProfiles.AsNoTracking().Where(p => p.UserId != user.Id)
            .OrderBy(p => p.DisplayName)
            .Select(p => new PasswordAccount(p.UserId, p.DisplayName, p.LoginEmail)).ToListAsync(ct);

    [HttpPost("reset"), Authorize(Policy = "Admin")]
    public Task<IActionResult> Reset(ResetPasswordInput input, CancellationToken ct) => Handle(async () =>
    {
        if (input.UserId == Guid.Empty || input.UserId == user.Id)
            return Problem(statusCode: 400, detail: "Välj en kollega. Använd Byt lösenord för ditt eget konto.");
        if (!await db.UserProfiles.AnyAsync(p => p.UserId == input.UserId, ct))
            return NotFound();
        if (!Configured()) return Unavailable();
        if (!await VerifyPassword(input.AdminPassword, ct))
            return Problem(statusCode: 400, detail: "Ditt nuvarande lösenord kunde inte verifieras.");

        // Generated only on the server; never stored in the application database or logs.
        var password = "Aa1!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        using var response = await Update(input.UserId, password, true, ct);
        if (!response.IsSuccessStatusCode) return ProviderFailure(response);
        return Ok(new TemporaryPassword(password));
    });

    [HttpPost("change")]
    public Task<IActionResult> Change(ChangePasswordInput input, CancellationToken ct) => Handle(async () =>
    {
        if (input.CurrentPassword == input.NewPassword)
            return Problem(statusCode: 400, detail: "Välj ett annat lösenord än det nuvarande.");
        if (!Configured()) return Unavailable();
        if (!await VerifyPassword(input.CurrentPassword, ct))
            return Problem(statusCode: 400, detail: "Ditt nuvarande lösenord kunde inte verifieras.");
        using var response = await Update(user.Id, input.NewPassword, false, ct);
        return response.IsSuccessStatusCode ? NoContent() : ProviderFailure(response);
    });

    private bool Configured() => options.Value.IsConfigured && !string.IsNullOrWhiteSpace(options.Value.SecretKey);
    private async Task<bool> VerifyPassword(string password, CancellationToken ct)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return false;
        using var request = RequestFor(HttpMethod.Post, "token?grant_type=password", false);
        request.Content = JsonContent.Create(new { email, password });
        using var response = await clients.CreateClient("SupabasePasswords").SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return false;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        // A successful password grant includes the authoritative user, not a client-supplied ID.
        return json.RootElement.TryGetProperty("user", out var account)
            && account.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var verified)
            && verified == user.Id;
    }
    private async Task<HttpResponseMessage> Update(Guid id, string password, bool temporary, CancellationToken ct)
    {
        using var request = RequestFor(HttpMethod.Put, "admin/users/" + id, true);
        // Supabase merges app_metadata; existing roles and unrelated metadata are retained.
        request.Content = JsonContent.Create(new { password, app_metadata = new { pizza_password_change_required = temporary } });
        return await clients.CreateClient("SupabasePasswords").SendAsync(request, ct);
    }
    private HttpRequestMessage RequestFor(HttpMethod method, string path, bool admin)
    {
        var config = options.Value;
        var request = new HttpRequestMessage(method, config.Url.TrimEnd('/') + "/auth/v1/" + path);
        var key = admin ? config.SecretKey : config.PublishableKey;
        request.Headers.Add("apikey", key);
        // Legacy service_role keys are JWTs; new sb_secret_ keys belong only in apikey.
        if (admin && !key.StartsWith("sb_secret_", StringComparison.Ordinal))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return request;
    }
    private ObjectResult ProviderFailure(HttpResponseMessage response) => Problem(
        statusCode: (int)response.StatusCode == 422 ? 400 : 503,
        detail: (int)response.StatusCode == 422
            ? "Lösenordet godkändes inte. Välj ett längre, unikt lösenord."
            : "Lösenordsändringen kunde inte bekräftas. Försök igen; vid återställning skapas då ett nytt lösenord.");
    private ObjectResult Unavailable() => Problem(statusCode: 503,
        detail: "Lösenordshantering är inte konfigurerad. Administratören behöver ange Supabase:SecretKey på servern.");
    private async Task<IActionResult> Handle(Func<Task<IActionResult>> action)
    {
        Response.Headers.CacheControl = "no-store";
        try { return await action(); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        { return Problem(statusCode: 503, detail: "Lösenordsändringen kunde inte bekräftas. Försök logga in med det nya lösenordet eller gör en ny återställning."); }
    }
}
