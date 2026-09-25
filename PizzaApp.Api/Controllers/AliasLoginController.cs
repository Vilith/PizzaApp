using System.Net.Http.Headers;
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

[ApiController, AllowAnonymous, Route("api/auth/login")]
[EnableRateLimiting("Registration"), RequestSizeLimit(4096)]
public sealed class AliasLoginController(PizzaDbContext db, IOptions<SupabaseOptions> options, IHttpClientFactory clients) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login(AliasLogin input, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!options.Value.IsConfigured) return Problem(statusCode: 503, detail: "Inloggningen är inte konfigurerad.");
        var key = PizzaDbContext.NormalizeAlias(input.Alias);
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.AliasKey == key, ct);
        if (string.IsNullOrWhiteSpace(profile?.LoginEmail)) return Rejected();
        try
        {
            using var request = ProviderRequest(HttpMethod.Post, "token?grant_type=password");
            request.Content = JsonContent.Create(new { email = profile.LoginEmail, password = input.Password });
            using var response = await clients.CreateClient("SupabaseRegistration").SendAsync(request, ct);
            if ((int)response.StatusCode == 429) return StatusCode(429);
            if ((int)response.StatusCode >= 500) return Unavailable();
            if (!response.IsSuccessStatusCode) return Rejected();
            using var session = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var tokens = session.RootElement;
            var access = tokens.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(access)) return Rejected();
            using var verify = ProviderRequest(HttpMethod.Get, "user");
            verify.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
            using var verified = await clients.CreateClient("SupabaseRegistration").SendAsync(verify, ct);
            if (!verified.IsSuccessStatusCode) return Rejected();
            using var json = JsonDocument.Parse(await verified.Content.ReadAsStringAsync(ct));
            var user = json.RootElement;
            if (!user.TryGetProperty("id", out var id) || !Guid.TryParse(id.GetString(), out var userId) || userId != profile.UserId
                || !user.TryGetProperty("email_confirmed_at", out var confirmed) || confirmed.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(confirmed.GetString())
                || (user.TryGetProperty("is_anonymous", out var anonymous) && anonymous.ValueKind == JsonValueKind.True)) return Rejected();
            string? role = null;
            if (user.TryGetProperty("app_metadata", out var metadata) && metadata.TryGetProperty("pizza_role", out var roleValue) && roleValue.ValueKind != JsonValueKind.Null)
            {
                if (roleValue.ValueKind != JsonValueKind.String || roleValue.GetString() is not ("admin" or "member")) return Rejected();
                role = roleValue.GetString();
            }
            if (role == null && !profile.IsRegisteredMember) return Rejected();
            // Return only session credentials; never expose the alias-to-email mapping.
            return Ok(new { access_token = access, refresh_token = tokens.GetProperty("refresh_token").GetString(), expires_in = tokens.GetProperty("expires_in").GetInt32() });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or KeyNotFoundException)
        { return Unavailable(); }
    }

    private HttpRequestMessage ProviderRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, options.Value.Url.TrimEnd('/') + "/auth/v1/" + path);
        request.Headers.Add("apikey", options.Value.PublishableKey);
        return request;
    }
    private ObjectResult Rejected() => Problem(statusCode: 401, detail: "Fel nick/alias eller lösenord, eller kontot är inte aktiverat.");
    private ObjectResult Unavailable() => Problem(statusCode: 503, detail: "Inloggningen kunde inte nås. Försök igen senare.");
}
