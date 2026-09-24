using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PizzaApp.Shared;

namespace PizzaApp.Services;

// Tokens only live in memory. No password or session is written to local files or
// browser storage. Closing the app requires signing in again.
public sealed class AuthService(IHttpClientFactory clients) : IAuthService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AuthConfiguration? config;
    private TokenResponse? tokens;
    private DateTimeOffset expiresAt;
    private int sessionVersion;
    public SignedInUser? User { get; private set; }
    public event Action? Changed;

    public async Task SignInAsync(string email, string password)
    {
        await gate.WaitAsync();
        try
        {
            ClearSession();
            var version = sessionVersion;
            using var configurationResponse = await clients.CreateClient("PublicApi").GetAsync("api/auth/config");
            if (configurationResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new AuthException("Inloggningen är inte konfigurerad ännu. Kontakta administratören.");
            configurationResponse.EnsureSuccessStatusCode();
            config = await configurationResponse.Content.ReadFromJsonAsync<AuthConfiguration>()
                ?? throw new AuthException("Inloggningen är inte konfigurerad.");
            if (!Uri.TryCreate(config.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                throw new AuthException("Inloggningen kräver en säker anslutning.");
            var signedInTokens = await RequestTokensAsync("password", new { email = email.Trim(), password });
            var signedInUser = await LoadUserAsync(signedInTokens.AccessToken);
            if (version != sessionVersion) throw new AuthException("Inloggningen avbröts. Försök igen.");
            tokens = signedInTokens;
            User = signedInUser;
            Changed?.Invoke();
        }
        catch { ClearSession(); throw; }
        finally { gate.Release(); }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (tokens == null || User == null) return null;
            if (DateTimeOffset.UtcNow >= expiresAt.AddSeconds(-60))
            {
                try
                {
                    var version = sessionVersion;
                    var refreshed = await RequestTokensAsync("refresh_token", new { refresh_token = tokens.RefreshToken });
                    var refreshedUser = await LoadUserAsync(refreshed.AccessToken);
                    if (version != sessionVersion) return null;
                    tokens = refreshed;
                    User = refreshedUser;
                    Changed?.Invoke();
                }
                catch (AuthException) { ClearSession(); throw; }
            }
            return tokens?.AccessToken;
        }
        finally { gate.Release(); }
    }

    private async Task<TokenResponse> RequestTokensAsync(string grant, object payload)
    {
        using var request = AuthRequest(HttpMethod.Post, "token?grant_type=" + grant);
        request.Content = JsonContent.Create(payload);
        using var response = await clients.CreateClient("SupabaseAuth").SendAsync(request);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AuthException(grant == "password" ? "Fel e-postadress eller lösenord, eller kontot är inte aktiverat." : "Din inloggning har gått ut. Logga in igen.");
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new AuthException("För många försök. Vänta en stund och försök igen.");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (result == null || string.IsNullOrEmpty(result.AccessToken) || string.IsNullOrEmpty(result.RefreshToken) || result.ExpiresIn <= 0)
            throw new AuthException("Inloggningssvaret kunde inte läsas. Försök igen.");
        expiresAt = DateTimeOffset.UtcNow.AddSeconds(result.ExpiresIn);
        return result;
    }

    private async Task<SignedInUser> LoadUserAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await clients.CreateClient("PublicApi").SendAsync(request);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AuthException("Kontot saknar tillgång till PizzaApp. Kontakta administratören.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SignedInUser>() ?? throw new AuthException("Inloggningssvaret kunde inte läsas.");
    }

    public async Task SignOutAsync()
    {
        await gate.WaitAsync();
        try
        {
            var accessToken = tokens?.AccessToken;
            ClearSession();
            if (config == null || accessToken == null) return;
            using var request = AuthRequest(HttpMethod.Post, "logout?scope=local");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await clients.CreateClient("SupabaseAuth").SendAsync(request);
            // Local credentials are already removed even if the network is unavailable.
            if (response.StatusCode != HttpStatusCode.Unauthorized) response.EnsureSuccessStatusCode();
        }
        finally { gate.Release(); }
    }

    public void ClearSession()
    {
        Interlocked.Increment(ref sessionVersion);
        tokens = null; User = null;
        Changed?.Invoke();
    }

    private HttpRequestMessage AuthRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, config!.Url.TrimEnd('/') + "/auth/v1/" + path);
        request.Headers.Add("apikey", config.PublishableKey);
        return request;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    }
}
