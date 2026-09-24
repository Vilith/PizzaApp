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
    public Task SignInAsync(string email, string password)
    { User = new(new TestUser().Id, email, false, "Anna"); Changed?.Invoke(); return Task.CompletedTask; }
    public Task UpdateProfileAsync(ProfileInput input)
    {
        User = User! with { DisplayName = input.DisplayName.Trim(), AvatarDataUrl = input.AvatarDataUrl, ProfileRevision = Guid.NewGuid() };
        Changed?.Invoke(); return Task.CompletedTask;
    }
    public Task RefreshUserAsync() { Changed?.Invoke(); return Task.CompletedTask; }
    public Task SignOutAsync() { ClearSession(); return Task.CompletedTask; }
    public void ClearSession() { User = null; Changed?.Invoke(); }
    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(User == null ? null : "test-token");
}
