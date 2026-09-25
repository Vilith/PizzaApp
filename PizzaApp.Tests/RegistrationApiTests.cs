using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PizzaApp.Api;
using PizzaApp.Api.Data;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class RegistrationApiTests
{
    [Theory]
    [InlineData("email_address_not_authorized", "Confirm email")]
    [InlineData("signup_disabled", "avstängd")]
    [InlineData("weak_password", "lösenordskrav")]
    [InlineData("captcha_failed", "CAPTCHA")]
    [InlineData("unknown_error", "Kunde inte slutföra")]
    public async Task Signup_reports_actionable_provider_errors_without_exposing_raw_messages(string code, string expected)
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.SignupError = code;
        var response = await client.PostAsJsonAsync("/api/registration/start", Input());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(expected, body);
        Assert.DoesNotContain("private-provider-detail", body);
    }

    private static RegistrationInput Input() => new() { Email = "anna@example.test", Password = "test-password", DisplayName = "Pizzafan" };
    private static void Login(HttpClient client) => client.DefaultRequestHeaders.Authorization = new("Bearer", "verified-token");

    [Fact]
    public async Task Registration_immediately_allows_login_and_ordering_without_email_or_admin_approval()
    {
        using var app = new App(); using var client = app.CreateClient();
        var start = await client.PostAsJsonAsync("/api/registration/start", Input());
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);
        Assert.DoesNotContain("pizza_role", app.Provider.LastSignup!);
        Assert.Contains("display_name", app.Provider.LastSignup!);
        Login(client);

        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.Equal("Pizzafan", user.DisplayName);
        Assert.False(user.IsAdmin);
        var order = await client.PostAsJsonAsync("/api/restaurants/1/orders", new OrderInput { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Vatten" });
        order.EnsureSuccessStatusCode();
        var saved = (await order.Content.ReadFromJsonAsync<OrderDetails>())!;
        Assert.Equal(user.Id, saved.OwnerUserId);
        Assert.Equal("Pizzafan", saved.Name);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/restaurants/1/orders/complete", new CompleteDayInput(saved.OrderDate, []))).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<PizzaDbContext>().UserProfiles.SingleAsync()).IsRegisteredMember);
    }

    [Fact]
    public async Task Invalid_email_is_rejected_on_every_registration_route_before_contacting_provider()
    {
        using var app = new App(); using var client = app.CreateClient();
        var input = Input(); input.Email = "invalid";
        foreach (var route in new[] { "start", "activate" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registration/" + route, input)).StatusCode);
        Assert.Equal(0, app.Provider.Calls);
    }

    [Fact]
    public async Task Signup_without_session_does_not_grant_membership()
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.OmitSignupSession = true;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registration/start", Input())).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<PizzaDbContext>().UserProfiles);
    }
    [Fact]
    public async Task Direct_provider_signup_requires_verified_activation_and_recovery_is_idempotent()
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.Confirmed = true;
        Login(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var wrongPassword = Input(); wrongPassword.Password = "wrong-password";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registration/activate", wrongPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/registration/activate", Input())).StatusCode);
        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        (await client.PutAsJsonAsync("/api/auth/profile", new ProfileInput { DisplayName = "Nytt nick", Revision = user.ProfileRevision })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/registration/activate", Input())).StatusCode);
        Assert.Equal("Nytt nick", (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.DisplayName);
        using var scope = app.Services.CreateScope();
        Assert.Single(scope.ServiceProvider.GetRequiredService<PizzaDbContext>().UserProfiles);
    }

    [Fact]
    public async Task Blocked_accounts_cannot_reactivate_and_registration_cannot_grant_admin()
    {
        using var app = new App(); using var client = app.CreateClient();
        var forged = new { Email = "anna@example.test", Password = "test-password", DisplayName = "Nick",
            IsAdmin = true, Role = "admin", UserId = Guid.NewGuid(), IsRegisteredMember = true };
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/registration/start", forged)).StatusCode);
        Login(client);
        Assert.False((await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.IsAdmin);
        app.Provider.Role = "disabled";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/registration/activate", Input())).StatusCode);
    }

    [Fact]
    public async Task Profile_endpoint_cannot_set_membership_and_existing_admin_is_not_downgraded()
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.Confirmed = true; app.Provider.Role = "member";
        Login(client);
        (await client.PutAsJsonAsync("/api/auth/profile", new { DisplayName = "Nick", IsRegisteredMember = true })).EnsureSuccessStatusCode();
        app.Provider.Role = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        app.Provider.Role = "admin";
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/registration/activate", Input())).StatusCode);
        Assert.True((await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.IsAdmin);
    }

    [Fact]
    public async Task Enabled_email_confirmation_is_rejected_before_signup()
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.AutoConfirm = false;
        var result = await client.PostAsJsonAsync("/api/registration/start", Input());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Contains("Confirm email", await result.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/registration/activate", Input())).StatusCode);
        Assert.Null(app.Provider.LastSignup);
    }

    [Fact]
    public async Task Provider_email_must_match_and_no_secrets_are_in_public_config()
    {
        using var app = new App(); using var client = app.CreateClient();
        app.Provider.Email = "someone-else@example.test";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registration/start", Input())).StatusCode);
        var config = await client.GetStringAsync("/api/auth/config");
        Assert.DoesNotContain("InvitationCode", config);
    }

    [Fact]
    public async Task Alias_login_accepts_case_and_whitespace_but_rejects_wrong_password_unknown_and_blocked_accounts()
    {
        using var app = new App(); using var client = app.CreateClient();
        (await client.PostAsJsonAsync("/api/registration/start", Input())).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/auth/login", new AliasLogin { Alias = "  pIzZaFaN  ", Password = "test-password" });
        response.EnsureSuccessStatusCode();
        Assert.Contains("verified-token", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("anna@example.test", await response.Content.ReadAsStringAsync());
        foreach (var input in new[] { new AliasLogin { Alias = "unknown", Password = "test-password" }, new AliasLogin { Alias = "Pizzafan", Password = "wrong" } })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", input)).StatusCode);
        app.Provider.Role = "disabled";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new AliasLogin { Alias = "Pizzafan", Password = "test-password" })).StatusCode);
        app.Provider.Role = "member";
        app.Provider.UserId = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new AliasLogin { Alias = "Pizzafan", Password = "test-password" })).StatusCode);
    }

    [Fact]
    public async Task Alias_is_unique_in_database_signup_and_rename_and_new_alias_is_used_for_login()
    {
        using var app = new App(); using var client = app.CreateClient();
        (await client.PostAsJsonAsync("/api/registration/start", Input())).EnsureSuccessStatusCode();
        var duplicate = Input(); duplicate.DisplayName = " pizzafan ";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/registration/start", duplicate)).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();
            db.UserProfiles.Add(new() { UserId = Guid.NewGuid(), DisplayName = "PIZZAFAN" });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();
            db.UserProfiles.Add(new() { UserId = Guid.NewGuid(), DisplayName = "Upptagen" });
            await db.SaveChangesAsync();
        }
        Login(client);
        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/auth/profile", new ProfileInput { DisplayName = " upptagen ", Revision = user.ProfileRevision })).StatusCode);
        (await client.PutAsJsonAsync("/api/auth/profile", new ProfileInput { DisplayName = "Nytt nick", Revision = user.ProfileRevision })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new AliasLogin { Alias = "Pizzafan", Password = "test-password" })).StatusCode);
        (await client.PostAsJsonAsync("/api/auth/login", new AliasLogin { Alias = "NYTT NICK", Password = "test-password" })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_attempts_are_rate_limited()
    {
        using var app = new App(); using var client = app.CreateClient();
        var input = new AliasLogin { Alias = "unknown", Password = "wrong" };
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", input)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/login", input)).StatusCode);
    }

    [Fact]
    public async Task Registration_attempts_are_rate_limited()
    {
        using var app = new App(); using var client = app.CreateClient();
        var input = Input(); input.Email = "invalid";
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registration/start", input)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/registration/start", input)).StatusCode);
    }

    private sealed class App : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public Provider Provider { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.Configure<SupabaseOptions>(o => { o.Url = "https://auth.example.test"; o.PublishableKey = "sb_publishable_test"; });
                services.AddHttpClient("SupabaseRegistration").ConfigurePrimaryHttpMessageHandler(() => Provider);
                services.AddHttpClient("SupabaseVerification").ConfigurePrimaryHttpMessageHandler(() => Provider);
                services.RemoveAll<DbContextOptions<PizzaDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PizzaDbContext>>();
                connection.Open();
                services.AddDbContext<PizzaDbContext>(o => o.UseSqlite(connection));
            });
        }
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PizzaDbContext>().Database.EnsureCreated();
            return host;
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
    }

    private sealed class Provider : HttpMessageHandler
    {
        public int Calls;
        public bool Confirmed, OmitSignupSession;
        public bool AutoConfirm = true;
        public string? Role, LastSignup;
        public string? SignupError;
        public string Email = "anna@example.test";
        public Guid UserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal("sb_publishable_test", Assert.Single(request.Headers.GetValues("apikey")));
            var path = request.RequestUri!.PathAndQuery;
            if (path == "/auth/v1/settings") return Json(new { mailer_autoconfirm = AutoConfirm });
            if (path == "/auth/v1/signup")
            {
                LastSignup = await request.Content!.ReadAsStringAsync(cancellationToken);
                if (SignupError != null) return new(HttpStatusCode.UnprocessableEntity)
                { Content = JsonContent.Create(new { error_code = SignupError, msg = "private-provider-detail" }) };
                Confirmed = AutoConfirm;
                return OmitSignupSession ? Json(new { }) : Json(new { access_token = "verified-token" });
            }
            if (path == "/auth/v1/token?grant_type=password")
            {
                using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                if (!Confirmed || json.RootElement.GetProperty("password").GetString() != "test-password") return new(HttpStatusCode.BadRequest);
                return Json(new { access_token = "verified-token", refresh_token = "refresh-test", expires_in = 3600 });
            }
            if (path == "/auth/v1/user")
            {
                if (request.Headers.Authorization?.Parameter != "verified-token") return new(HttpStatusCode.Unauthorized);
                return Json(new { id = UserId, email = Email,
                    email_confirmed_at = Confirmed ? "2026-09-24T10:00:00Z" : null, app_metadata = new { pizza_role = Role },
                    user_metadata = new { pizza_role = "admin", IsRegisteredMember = true }, is_anonymous = false });
            }
            throw new InvalidOperationException("Unexpected provider request: " + path);
        }
        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    }
}
