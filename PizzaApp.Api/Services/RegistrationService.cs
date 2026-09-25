using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

public sealed class RegistrationService(IOptions<SupabaseOptions> supabase,
    IHttpClientFactory clients, PizzaDbContext db)
{
    private void CheckConfiguration()
    {
        if (!supabase.Value.IsConfigured)
            throw new OrderException(503, "Registreringen är inte konfigurerad ännu. Kontakta administratören.");
    }

    private static string Name(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name.Any(char.IsControl))
            throw new OrderException(400, "Ange ett nick utan radbrytningar, högst 100 tecken.");
        return name;
    }

    public async Task StartAsync(RegistrationInput input, CancellationToken cancellationToken)
    {
        CheckConfiguration();
        var name = Name(input.DisplayName);
        await CheckAliasAsync(name, null, cancellationToken);
        await CheckDirectSignupAsync(cancellationToken);
        // No role is sent as editable user_metadata.
        using var response = await PostAsync("signup", new { email = input.Email.Trim(), password = input.Password,
            data = new { display_name = name } }, cancellationToken);
        await EnsureSuccessAsync(response);
        await EnrollAsync(response, input.Email, name, cancellationToken);
    }

    // Recovery after signup succeeded but profile creation or the response failed.
    public async Task ActivateAsync(RegistrationInput input, CancellationToken cancellationToken)
    {
        CheckConfiguration();
        var name = Name(input.DisplayName);
        await CheckDirectSignupAsync(cancellationToken);
        using var response = await PostAsync("token?grant_type=password", new { email = input.Email.Trim(), password = input.Password }, cancellationToken);
        await EnsureSuccessAsync(response);
        await EnrollAsync(response, input.Email, name, cancellationToken);
    }

    private async Task EnrollAsync(HttpResponseMessage response, string email, string name, CancellationToken cancellationToken)
    {
        using var session = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!session.RootElement.TryGetProperty("access_token", out var token) || token.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(token.GetString()))
            throw new OrderException(400, "Kontot kunde inte aktiveras med registreringstjänstens svar. Välj Slutför befintligt konto och försök igen.");
        using var request = Request(HttpMethod.Get, "user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.GetString());
        using var verified = await clients.CreateClient("SupabaseRegistration").SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(verified);
        using var json = JsonDocument.Parse(await verified.Content.ReadAsStringAsync(cancellationToken));
        var user = json.RootElement;
        if (!user.TryGetProperty("id", out var id) || !Guid.TryParse(id.GetString(), out var userId) || userId == Guid.Empty
            || !user.TryGetProperty("email", out var verifiedEmail) || !string.Equals(verifiedEmail.GetString(), email.Trim(), StringComparison.OrdinalIgnoreCase)
            || !user.TryGetProperty("email_confirmed_at", out var confirmed) || confirmed.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(confirmed.GetString())
            || (user.TryGetProperty("is_anonymous", out var anonymous) && anonymous.ValueKind == JsonValueKind.True))
            throw new OrderException(400, "Kontot kunde inte aktiveras med registreringstjänstens svar. Välj Slutför befintligt konto och försök igen.");
        // An explicit blocked/unknown role cannot be bypassed through registration.
        if (user.TryGetProperty("app_metadata", out var metadata)
            && metadata.TryGetProperty("pizza_role", out var role) && role.ValueKind != JsonValueKind.Null
            && (role.ValueKind != JsonValueKind.String || role.GetString() is not ("member" or "admin")))
            throw new OrderException(403, "Kontot kan inte aktiveras. Kontakta administratören.");
        var profile = await db.UserProfiles.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        await CheckAliasAsync(profile?.DisplayName ?? name, userId, cancellationToken);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId, DisplayName = name, Revision = Guid.NewGuid() };
            db.UserProfiles.Add(profile);
        }
        // Retries never overwrite an existing nickname, picture or administrator role.
        profile.LoginEmail = verifiedEmail.GetString();
        profile.IsRegisteredMember = true;
        profile.Revision = Guid.NewGuid();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        { throw new OrderException(409, "Aliaset kan vara upptaget. Välj Slutför befintligt konto och försök igen med ett annat nick."); }
    }

    private async Task CheckAliasAsync(string name, Guid? owner, CancellationToken ct)
    {
        var key = PizzaDbContext.NormalizeAlias(name);
        if (await db.UserProfiles.AnyAsync(p => p.AliasKey == key && p.UserId != owner, ct))
            throw new OrderException(409, "Det nick/alias du valt är upptaget. Välj ett annat.");
    }

    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, supabase.Value.Url.TrimEnd('/') + "/auth/v1/" + path);
        request.Headers.Add("apikey", supabase.Value.PublishableKey);
        return request;
    }

    private async Task CheckDirectSignupAsync(CancellationToken ct)
    {
        using var request = Request(HttpMethod.Get, "settings");
        using var response = await clients.CreateClient("SupabaseRegistration").SendAsync(request, ct);
        await EnsureSuccessAsync(response);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!json.RootElement.TryGetProperty("mailer_autoconfirm", out var autoConfirm) || autoConfirm.ValueKind != JsonValueKind.True)
            throw new OrderException(503, "Stäng av Confirm email i Supabase under Authentication → Sign In / Providers → Email för att skapa konton utan mejlkod.");
    }

    private async Task<HttpResponseMessage> PostAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = Request(HttpMethod.Post, path);
        request.Content = JsonContent.Create(payload);
        return await clients.CreateClient("SupabaseRegistration").SendAsync(request, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        string? code = null;
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var field in new[] { "error_code", "code" })
                    if (json.RootElement.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
                    { code = value.GetString(); break; }
        }
        catch (JsonException) { /* A proxy may return HTML instead of a provider error. */ }
        // Translate only known codes. Raw provider messages may contain private data.
        var detail = code switch
        {
            "email_address_not_authorized" => "Supabase försöker skicka ett bekräftelsemejl. Administratören behöver stänga av Confirm email för registrering utan mejlkod.",
            "signup_disabled" or "email_provider_disabled" => "Registrering med e-post är avstängd i Supabase. Administratören behöver aktivera registrering och e-post/lösenord.",
            "weak_password" => "Lösenordet uppfyller inte Supabases lösenordskrav. Välj ett starkare lösenord.",
            "email_address_invalid" => "Supabase godkänner inte e-postadressen. Kontrollera att du använder en riktig e-postadress.",
            "email_not_confirmed" => "Det äldre kontot väntar på e-postbekräftelse i Supabase. Kontakta administratören för att aktivera det.",
            "otp_expired" => "Bekräftelsekoden är ogiltig eller har gått ut. Begär en ny kod och försök igen.",
            "captcha_failed" => "Supabase kräver en CAPTCHA-kontroll som appen ännu inte stöder. Kontakta administratören.",
            "over_email_send_rate_limit" => "Gränsen för bekräftelsemejl har nåtts. Vänta en stund innan du begär ett nytt mejl.",
            _ => null
        };
        if (detail != null)
            throw new OrderException(code == "over_email_send_rate_limit" ? 429 : 400, detail);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new OrderException(429, "För många försök. Vänta en stund och försök igen.");
        if ((int)response.StatusCode >= 500)
            throw new OrderException(503, "Registreringstjänsten kunde inte nås. Försök igen senare.");
        if (!response.IsSuccessStatusCode)
            throw new OrderException(400, "Kunde inte slutföra registreringen. Kontrollera uppgifterna. Har du redan ett konto kan du logga in eller välja Slutför befintligt konto.");
    }
}
