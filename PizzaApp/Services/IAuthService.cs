using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface IAuthService
{
    SignedInUser? User { get; }
    event Action? Changed;
    Task SignInAsync(string alias, string password);
    Task SignOutAsync();
    Task UpdateProfileAsync(ProfileInput input);
    Task RefreshUserAsync();
    Task RegisterAsync(RegistrationInput input);
    Task ActivateRegistrationAsync(RegistrationInput input);
    Task<string?> GetAccessTokenAsync();
    Task<List<PasswordAccount>> GetPasswordAccountsAsync();
    Task<TemporaryPassword> ResetPasswordAsync(ResetPasswordInput input);
    Task ChangePasswordAsync(ChangePasswordInput input);
    void ClearSession();
}

public sealed class AuthException(string message) : Exception(message);
