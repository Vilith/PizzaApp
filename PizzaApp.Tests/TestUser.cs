// Testidentitet och delade testdata för behörighetskontroller.

using PizzaApp.Api.Services;
using PizzaApp.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public sealed class TestUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public bool IsAdmin { get; set; } = true;
}

public sealed class FakeAuth : IAuthService
{
    public SignedInUser? User { get; set; } = new(new TestUser().Id, "admin@example.test", true, "Anna");
    public event Action? Changed;

    // Inloggning: verifiera alias och starta session.
    public Task SignInAsync(string email, string password)
    { User = new(new TestUser().Id, email, false, "Anna"); Changed?.Invoke(); return Task.CompletedTask; }

    // Profil: validera och spara användarens ändringar.
    public Task UpdateProfileAsync(ProfileInput input)
    {
        User = User! with { DisplayName = input.DisplayName.Trim(), AvatarDataUrl = input.AvatarDataUrl, ProfileRevision = Guid.NewGuid() };
        Changed?.Invoke(); return Task.CompletedTask;
    }

    // Profil: hämta senaste användaruppgifterna.
    public Task RefreshUserAsync() { Changed?.Invoke(); return Task.CompletedTask; }
    public bool Registered, Activated;
    public bool FailRegistration;
    public Task RegisterAsync(RegistrationInput input)
    { if (FailRegistration) throw new AuthException("Registreringen misslyckades."); Registered = true; return Task.CompletedTask; }
    public Task ActivateRegistrationAsync(RegistrationInput input) { Activated = true; return Task.CompletedTask; }

    // Session: rensa lokal inloggningsstatus.

    // Utloggning: avsluta sessionen och rensa lokal användardata.
    public Task SignOutAsync() { ClearSession(); return Task.CompletedTask; }
    public List<PasswordAccount> Accounts = [new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Kollega", "kollega@example.test")];
    public ResetPasswordInput? LastReset;
    public bool PasswordChanged;
    public Task<List<PasswordAccount>> GetPasswordAccountsAsync() => Task.FromResult(Accounts);
    public Task<TemporaryPassword> ResetPasswordAsync(ResetPasswordInput input)
    {
        LastReset = new() { UserId = input.UserId, AdminPassword = input.AdminPassword };
        return Task.FromResult(new TemporaryPassword("test-temporary-password"));
    }
    public Task ChangePasswordAsync(ChangePasswordInput input)
    { PasswordChanged = true; ClearSession(); return Task.CompletedTask; }

    // Session: rensa lokal inloggningsstatus.
    public void ClearSession() { User = null; Changed?.Invoke(); }

    // Session: hämta eller förnya åtkomsttoken.
    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(User == null ? null : "test-token");
}
