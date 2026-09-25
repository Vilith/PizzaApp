using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PizzaApp.Components.Pages;
using PizzaApp.Services;

namespace PizzaApp.Tests;

public class RegistrationUiTests : TestContext
{
    private readonly FakeAuth auth = new() { User = null };
    public RegistrationUiTests() => Services.AddSingleton<IAuthService>(auth);

    private IRenderedComponent<Login> FillRegistration()
    {
        var page = RenderComponent<Login>();
        page.Find("[data-action='register']").Click();
        page.Find("#register-name").Change("Pizzafan");
        page.Find("#register-email").Change("anna@example.test");
        page.Find("#register-password").Change("test-password");
        Assert.Empty(page.FindAll("#invitation-code"));
        return page;
    }

    [Fact]
    public void Signup_completes_without_email_code_and_returns_to_login()
    {
        var page = FillRegistration();
        page.Find("form").Submit();
        Assert.True(auth.Registered);
        Assert.Null(auth.User);
        Assert.Empty(page.FindAll("#register-password"));

        Assert.Contains("Kontot är klart", page.Markup);
        Assert.Empty(page.FindAll("#confirmation-code"));
        page.Find(".btn-primary").Click();
        Assert.NotEmpty(page.FindAll("#login-alias"));
        Assert.Null(auth.User);
    }

    [Fact]
    public void Failed_registration_keeps_form_and_clears_password()
    {
        auth.FailRegistration = true;
        var page = FillRegistration();
        page.Find("form").Submit();
        Assert.Contains("Registreringen misslyckades", page.Markup);
        Assert.False(auth.Registered);
        Assert.Equal("", page.Find("#register-password").GetAttribute("value") ?? "");
        Assert.Equal("Pizzafan", page.Find("#register-name").GetAttribute("value"));
    }

    [Fact]
    public void Existing_account_can_recover_activation_with_password()
    {
        var page = FillRegistration();
        page.FindAll("button").Single(b => b.TextContent == "Slutför befintligt konto").Click();
        page.Find("form").Submit();
        Assert.True(auth.Activated);
        Assert.Contains("Kontot är klart", page.Markup);
    }
}
