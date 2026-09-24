using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface IAuthService
{
    SignedInUser? User { get; }
    event Action? Changed;
    Task SignInAsync(string email, string password);
    Task SignOutAsync();
    Task<string?> GetAccessTokenAsync();
    void ClearSession();
}

public sealed class AuthException(string message) : Exception(message);
