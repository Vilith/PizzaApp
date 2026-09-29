using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PizzaApp.Components.Pages;
using PizzaApp.Services;

namespace PizzaApp.Tests;

public class PasswordSettingsTests : TestContext
{
    private readonly FakeAuth auth = new();
    public PasswordSettingsTests() => Services.AddSingleton<IAuthService>(auth);

    [Fact]
    public void Admin_selects_colleague_reauthenticates_and_can_hide_temporary_password()
    {
        var page = RenderComponent<PasswordSettings>();
        page.Find("button.btn-outline-primary").Click();
        page.Find("#reset-account").Change(auth.Accounts[0].Id.ToString());
        page.Find("#admin-password").Change("admin-password");
        page.Find("form:has(#admin-password)").Submit();
        Assert.Equal(auth.Accounts[0].Id, auth.LastReset!.UserId);
        Assert.Equal("admin-password", auth.LastReset.AdminPassword);
        Assert.Contains("Kollega", page.Find("[role=status]").TextContent);
        Assert.Equal("test-temporary-password", page.Find("code").TextContent);
        Assert.Equal("", page.Find("#admin-password").GetAttribute("value") ?? "");
        page.Find("[role=status] button").Click();
        Assert.Empty(page.FindAll("code"));
    }

    [Fact]
    public void Member_cannot_see_admin_controls_and_must_confirm_new_password()
    {
        auth.User = auth.User! with { IsAdmin = false, MustChangePassword = true };
        var page = RenderComponent<PasswordSettings>();
        Assert.Contains("tillfälligt lösenord", page.Markup);
        Assert.Empty(page.FindAll("#reset-account, #admin-password, .btn-outline-primary"));
        page.Find("#current-password").Change("temporary-password");
        page.Find("#new-password").Change("new-password");
        page.Find("#confirm-password").Change("different");
        page.Find("form").Submit();
        Assert.False(auth.PasswordChanged);
        Assert.Contains("Lösenorden matchar inte", page.Markup);
        page.Find("#confirm-password").Change("new-password");
        page.Find("form").Submit();
        Assert.True(auth.PasswordChanged);
        Assert.Null(auth.User);
        Assert.Empty(page.FindAll("input"));
    }

    [Fact]
    public void Temporary_password_profile_shows_only_password_form()
    {
        auth.User = auth.User! with { MustChangePassword = true };
        var page = RenderComponent<ProfileSettings>();
        Assert.Empty(page.FindAll("#profile-name"));
        Assert.NotEmpty(page.FindAll("#current-password"));
        Assert.Empty(page.FindAll("#admin-password"));
    }

    [Fact]
    public void Login_does_not_play_door_animation()
    {
        auth.User = null;
        var page = RenderComponent<Login>();
        page.Find("#login-alias").Change("Anna");
        page.Find("#login-password").Change("my-password");
        page.Find("form").Submit();
        Assert.NotNull(auth.User);
        Assert.Empty(JSInterop.Invocations);
    }
}
