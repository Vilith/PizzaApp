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

public class PasswordApiTests
{
    private static readonly Guid Admin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Member = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("member", HttpStatusCode.Forbidden)]
    public async Task Reset_and_account_list_require_admin(string? token, HttpStatusCode expected)
    {
        using var app = new App(); using var client = app.Client(token);
        Assert.Equal(expected, (await client.GetAsync("/api/passwords/accounts")).StatusCode);
        Assert.Equal(expected, (await client.PostAsJsonAsync("/api/passwords/reset",
            new ResetPasswordInput { UserId = Member, AdminPassword = "correct-password" })).StatusCode);
        Assert.Equal(0, app.Provider.Updates);
    }

    [Fact]
    public async Task Reset_requires_admin_password_and_known_target_and_does_not_expose_key()
    {
        using var app = new App(); using var client = app.Client("admin");
        var accounts = await client.GetFromJsonAsync<List<PasswordAccount>>("/api/passwords/accounts");
        Assert.Equal(Member, Assert.Single(accounts!).Id);
        foreach (var input in new[] {
            new ResetPasswordInput { UserId = Member, AdminPassword = "wrong" },
            new ResetPasswordInput { UserId = Admin, AdminPassword = "correct-password" },
            new ResetPasswordInput { UserId = Guid.Empty, AdminPassword = "correct-password" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/passwords/reset", input)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/passwords/reset",
            new ResetPasswordInput { UserId = Guid.NewGuid(), AdminPassword = "correct-password" })).StatusCode);
        Assert.Equal(0, app.Provider.Updates);
        Assert.DoesNotContain("sb_secret_", await client.GetStringAsync("/api/auth/config"));
    }

    [Fact]
    public async Task Reset_forces_change_blocks_orders_and_change_preserves_profile_and_role()
    {
        using var app = new App(); using var admin = app.Client("admin"); using var member = app.Client("member");
        var response = await admin.PostAsJsonAsync("/api/passwords/reset",
            new ResetPasswordInput { UserId = Member, AdminPassword = "correct-password" });
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoStore);
        var temporary = (await response.Content.ReadFromJsonAsync<TemporaryPassword>())!.Password;
        Assert.True(temporary.Length >= 20);
        Assert.Equal(temporary, app.Provider.MemberPassword);
        Assert.True((await member.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.MustChangePassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/restaurants/1/menu")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsJsonAsync("/api/restaurants/1/orders",
            new OrderInput { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Fanta 33cl" })).StatusCode);

        var change = new ChangePasswordInput { CurrentPassword = temporary, NewPassword = "my-new-password", ConfirmPassword = "my-new-password" };
        (await member.PostAsJsonAsync("/api/passwords/change", change)).EnsureSuccessStatusCode();
        var user = (await member.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.False(user.MustChangePassword);
        Assert.Equal("Kollega", user.DisplayName);
        Assert.False(user.IsAdmin);
        Assert.Equal("my-new-password", app.Provider.MemberPassword);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/restaurants/1/menu")).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<PizzaDbContext>().UserProfiles.CountAsync());
    }

    [Fact]
    public async Task Invalid_current_password_mismatch_and_weak_password_do_not_update()
    {
        using var app = new App(); using var member = app.Client("member");
        foreach (var input in new[] {
            new ChangePasswordInput { CurrentPassword = "wrong", NewPassword = "new-password", ConfirmPassword = "new-password" },
            new ChangePasswordInput { CurrentPassword = "correct-password", NewPassword = "new-password", ConfirmPassword = "different" },
            new ChangePasswordInput { CurrentPassword = "correct-password", NewPassword = "short", ConfirmPassword = "short" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await member.PostAsJsonAsync("/api/passwords/change", input)).StatusCode);
        Assert.Equal(0, app.Provider.Updates);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_configuration_or_provider_failure_returns_no_password(bool missingKey)
    {
        using var app = new App(missingKey); using var admin = app.Client("admin");
        app.Provider.FailUpdate = !missingKey;
        var response = await admin.PostAsJsonAsync("/api/passwords/reset",
            new ResetPasswordInput { UserId = Member, AdminPassword = "correct-password" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Aa1!", await response.Content.ReadAsStringAsync());
        Assert.False(app.Provider.MustChange);
        Assert.Equal(missingKey ? 0 : 1, app.Provider.Updates);
    }

    private sealed class App(bool missingKey = false) : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public readonly Provider Provider = new();
        public HttpClient Client(string? token)
        {
            var client = CreateClient();
            if (token != null) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            return client;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.Configure<SupabaseOptions>(o => {
                    o.Url = "https://auth.example.test"; o.PublishableKey = "sb_publishable_test";
                    o.SecretKey = missingKey ? "" : "sb_secret_test";
                });
                services.AddHttpClient("SupabaseVerification").ConfigurePrimaryHttpMessageHandler(() => Provider);
                services.AddHttpClient("SupabasePasswords").ConfigurePrimaryHttpMessageHandler(() => Provider);
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
            var db = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();
            db.Database.EnsureCreated();
            db.UserProfiles.AddRange(
                new() { UserId = Admin, DisplayName = "Admin", LoginEmail = "admin@example.test", IsRegisteredMember = true },
                new() { UserId = Member, DisplayName = "Kollega", LoginEmail = "member@example.test", IsRegisteredMember = true });
            db.SaveChanges();
            return host;
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
    }
    private sealed class Provider : HttpMessageHandler
    {
        public int Updates;
        public bool MustChange, FailUpdate;
        public string MemberPassword = "correct-password";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/token"))
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                var admin = body.RootElement.GetProperty("email").GetString() == "admin@example.test";
                var password = body.RootElement.GetProperty("password").GetString();
                if (password != (admin ? "correct-password" : MemberPassword)) return new(HttpStatusCode.BadRequest);
                return Json(new { user = new { id = admin ? Admin : Member } });
            }
            if (path.Contains("/admin/users/"))
            {
                Assert.Equal("sb_secret_test", request.Headers.GetValues("apikey").Single());
                Assert.Null(request.Headers.Authorization);
                Assert.EndsWith(Member.ToString(), path);
                Updates++;
                if (FailUpdate) return new(HttpStatusCode.InternalServerError);
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                MemberPassword = body.RootElement.GetProperty("password").GetString()!;
                var metadata = body.RootElement.GetProperty("app_metadata");
                Assert.False(metadata.TryGetProperty("pizza_role", out _));
                MustChange = metadata.GetProperty("pizza_password_change_required").GetBoolean();
                return Json(new { id = Member });
            }
            var token = request.Headers.Authorization?.Parameter;
            if (token is not ("admin" or "member")) return new(HttpStatusCode.Unauthorized);
            return Json(new { id = token == "admin" ? Admin : Member,
                email = token + "@example.test", email_confirmed_at = "2026-01-01T00:00:00Z",
                app_metadata = new { pizza_role = token, pizza_password_change_required = token == "member" && MustChange } });
        }
        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    }
}
