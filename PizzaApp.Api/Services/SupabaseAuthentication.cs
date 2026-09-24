using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PizzaApp.Api.Services;

public class SupabaseOptions
{
    public string Url { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public bool IsConfigured => Uri.TryCreate(Url, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo)
        && PublishableKey.StartsWith("sb_publishable_", StringComparison.Ordinal);
}

// Verify with the project's Auth server, not untrusted claims decoded by the client.
// Reading the current app_metadata also makes membership/role changes effective
// on the next request instead of waiting for an old JWT to expire.
public sealed class SupabaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    IOptions<SupabaseOptions> supabase, IHttpClientFactory clients)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        if (string.IsNullOrWhiteSpace(header.Parameter) || header.Parameter.Length > 16384)
            return AuthenticateResult.Fail("Ogiltig inloggning.");
        var config = supabase.Value;
        if (!config.IsConfigured) return AuthenticateResult.Fail("Inloggning är inte konfigurerad.");
        using var request = new HttpRequestMessage(HttpMethod.Get, config.Url.TrimEnd('/') + "/auth/v1/user");
        request.Headers.Authorization = header;
        request.Headers.Add("apikey", config.PublishableKey);
        try
        {
            using var response = await clients.CreateClient("SupabaseVerification").SendAsync(request, Context.RequestAborted);
            if (!response.IsSuccessStatusCode) return AuthenticateResult.Fail("Inloggningen kunde inte verifieras.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));
            var user = json.RootElement;
            if (!user.TryGetProperty("id", out var id) || !Guid.TryParse(id.GetString(), out var userId)
                || userId == Guid.Empty
                || !user.TryGetProperty("email_confirmed_at", out var confirmed) || confirmed.ValueKind != JsonValueKind.String
                || (user.TryGetProperty("is_anonymous", out var anonymous) && anonymous.ValueKind == JsonValueKind.True)
                || !user.TryGetProperty("app_metadata", out var metadata)
                || !metadata.TryGetProperty("pizza_role", out var role) || role.ValueKind != JsonValueKind.String
                || role.GetString() is not ("member" or "admin"))
                return AuthenticateResult.Fail("Kontot har inte tillgång till PizzaApp.");
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, user.TryGetProperty("email", out var email) ? email.GetString() ?? "" : ""),
                new Claim(ClaimTypes.Role, role.GetString()!)
            };
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            return AuthenticateResult.Fail("Inloggningen kunde inte verifieras. Försök igen.");
        }
    }
}

public interface ICurrentUser
{
    Guid Id { get; }
    bool IsAdmin { get; }
}

public sealed class CurrentUser(IHttpContextAccessor context) : ICurrentUser
{
    public Guid Id => Guid.TryParse(context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new OrderException(401, "Logga in för att fortsätta.");
    public bool IsAdmin => context.HttpContext?.User.IsInRole("admin") == true;
}
