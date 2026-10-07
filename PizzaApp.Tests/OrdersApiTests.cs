using System.Net;
using System.Net.Http.Json;
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

public class OrdersApiTests
{
    [Fact]
    public async Task Client_service_uses_restaurant_routes_and_surfaces_deadline_errors()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var api = new PizzaApp.Services.OrderApiService(client);
        var order = await api.SaveAsync(1, Pizza());
        Assert.Single((await api.GetTodayAsync(1)).Orders);
        var edit = Pizza(); edit.Revision = order.Revision; edit.Quantity = 2;
        order = await api.SaveAsync(1, edit, order.Id);
        Assert.Equal(2, order.Quantity);
        await api.DeleteAsync(1, order.Id, order.Revision);
        Assert.Empty((await api.GetTodayAsync(1)).Orders);

        using var lockedApp = new TestApp(locked: true); using var lockedClient = lockedApp.CreateClient();
        var lockedApi = new PizzaApp.Services.OrderApiService(lockedClient);
        var error = await Assert.ThrowsAsync<PizzaApp.Services.OrderApiException>(() => lockedApi.SaveAsync(1, Pizza()));
        Assert.Contains("11:15", error.Message);
    }

    [Fact]
    public async Task Save_read_edit_and_delete_through_http()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var created = await client.PostAsJsonAsync("/api/restaurants/1/orders", Pizza());
        created.EnsureSuccessStatusCode();
        var order = (await created.Content.ReadFromJsonAsync<OrderDetails>())!;
        var list = (await client.GetFromJsonAsync<DailyOrderList>("/api/restaurants/1/orders"))!;
        Assert.Equal(order.Id, Assert.Single(list.Orders).Id);
        Assert.Equal("Vesuvio", Assert.Single(list.Summary).Pizza);
        var edit = Pizza(); edit.Quantity = 3; edit.Revision = order.Revision;
        var updated = await client.PutAsJsonAsync($"/api/restaurants/1/orders/{order.Id}", edit);
        updated.EnsureSuccessStatusCode();
        var revised = (await updated.Content.ReadFromJsonAsync<OrderDetails>())!;
        Assert.Equal(3, revised.Quantity);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/restaurants/1/orders/{order.Id}?revision={order.Revision}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/restaurants/1/orders/{order.Id}?revision={revised.Revision}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<DailyOrderList>("/api/restaurants/1/orders"))!.Orders);
    }

    [Fact]
    public async Task Invalid_payload_is_rejected_by_real_model_binding()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var invalid = Pizza(); invalid.Quantity = 0;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/restaurants/1/orders", invalid)).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<DailyOrderList>("/api/restaurants/1/orders"))!.Orders);
    }

    [Fact]
    public async Task Lock_cannot_be_bypassed_through_api_or_legacy_route()
    {
        using var app = new TestApp(locked: true); using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/restaurants/1/orders", Pizza());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("11:15", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/orders", Pizza())).StatusCode);
    }

    [Fact]
    public async Task Restaurant_and_menu_payloads_can_be_serialized_and_lists_stay_separate()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var restaurants = await client.GetFromJsonAsync<List<PizzaApp.Models.Restaurant>>("/api/restaurants");
        Assert.True(restaurants!.Single(r => r.Id == 1).IsPizzeria);
        var menu = await client.GetFromJsonAsync<List<PizzaApp.Models.MenuItem>>("/api/restaurants/1/menu");
        Assert.Equal(58, menu!.Count);
        Assert.DoesNotContain(menu, item => item.Id is 62 or 63 or 64);
        var pizzas = menu.Where(m => m.Category == "Pizzor").ToList();
        Assert.Equal(Enumerable.Range(1, 43), pizzas.Select(m => m.MenuNumber!.Value));
        Assert.All(pizzas, item =>
        {
            Assert.Equal(item.MenuNumber <= 12 ? 85m : item.MenuNumber == 36 ? 100m : 90m, item.Price);
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
        });
        Assert.Equal("Margherita", menu[0].Name);
        Assert.Equal("Flygande Tefat", menu[35].Name);
        Assert.Contains("Dubbel inbakad", menu[35].Description);
        Assert.Equal(new[] { "Pizzor", "Sallader", "Kebab", "Stekrätter" }, menu.Select(m => m.Category).Distinct());
        Assert.Equal(8, menu.Count(m => m.Category == "Sallader"));
        Assert.Equal(6, menu.Count(m => m.Category == "Kebab"));
        Assert.All(menu.Where(m => m.Category is "Sallader" or "Kebab"), item =>
        {
            Assert.Equal(90m, item.Price);
            Assert.Null(item.MenuNumber);
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
        });
        var burger = Assert.Single(menu, m => m.Category == "Stekrätter");
        Assert.Equal("Hamburgare 90gr", burger.Name);
        Assert.Equal(80m, burger.Price);
        var burgerOrder = Pizza(); burgerOrder.MenuItemId = burger.Id;
        var response = await client.PostAsJsonAsync("/api/restaurants/1/orders", burgerOrder);
        response.EnsureSuccessStatusCode();
        Assert.Equal(80m, (await response.Content.ReadFromJsonAsync<OrderDetails>())!.UnitPrice);
        var alaCarte = await client.GetFromJsonAsync<List<PizzaApp.Models.MenuItem>>("/api/restaurants/2/menu");
        Assert.Equal(new[] { 5, 6, 7, 8 }, alaCarte!.Select(m => m.Id));
        (await client.PostAsJsonAsync("/api/restaurants/1/orders", Pizza())).EnsureSuccessStatusCode();
        Assert.Empty((await client.GetFromJsonAsync<DailyOrderList>("/api/restaurants/2/orders"))!.Orders);
    }

    [Fact]
    public void Postgres_model_matches_migrations_and_upgrade_preserves_old_rows()
    {
        using var db = new PizzaDbContext(new DbContextOptionsBuilder<PizzaDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
            .GenerateScript("20260917191531_SeedMenuItemsTest");
        Assert.Contains("OrderDate", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DELETE FROM \"Orders\"", sql);
    }

    private static OrderInput Pizza() => new() { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Coca-Cola 33cl" };

    [Fact]
    public async Task Client_can_assign_collector_and_complete_day_without_duplicate_history()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var api = new PizzaApp.Services.OrderApiService(client);
        var input = Pizza(); input.Name = "Anna";
        var order = await api.SaveAsync(1, input);
        var day = await api.SetCollectorAsync(1, order.Id, new(true, order.Revision, order.OrderDate));
        var request = new CompleteDayInput(day.Date, day.Orders.ToDictionary(o => o.Id, o => o.Revision));
        var completed = await api.CompleteAsync(1, request);
        Assert.NotNull(completed.CollectedAt);
        Assert.Equal(completed.CollectedAt, (await api.CompleteAsync(1, request)).CollectedAt);
        Assert.Equal("Anna", Assert.Single(completed.CollectedBy));
        await Assert.ThrowsAsync<PizzaApp.Services.OrderApiException>(() => api.SaveAsync(1, Pizza()));
    }

    [Fact]
    public async Task Sperring_admin_publishes_two_weeks_with_conflicts_and_member_permissions()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var page = (await client.GetFromJsonAsync<SperringPage>("/api/sperring"))!;
        Assert.Equal(new[] { 39, 40 }, page.Menus.Select(m => m.Week));
        var menu = page.Menus[0];
        menu.RegularDishes = "Köttbullar\nVegetarisk pasta";
        menu.Days[1] = "Tisdagssoppa";
        client.DefaultRequestHeaders.Authorization = new("Bearer", "member");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/sperring/1", menu)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "admin");
        (await client.PutAsJsonAsync("/api/sperring/1", menu)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/sperring/1", menu)).StatusCode);
        page = (await client.GetFromJsonAsync<SperringPage>("/api/sperring"))!;
        Assert.Equal("Tisdagssoppa", page.Menus[0].Days[1]);
        var next = page.Menus[1]; next.Week = 39;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/sperring/2", next)).StatusCode);
        next.Week = 53; // 2026 has 53 ISO weeks; 2025 does not.
        next.Year = 2025;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/sperring/2", next)).StatusCode);
        next.Year = 2026; next.Week = 40; next.RegularDishes = "Nästa veckas fisk";
        (await client.PutAsJsonAsync("/api/sperring/2", next)).EnsureSuccessStatusCode();
        page = (await client.GetFromJsonAsync<SperringPage>("/api/sperring"))!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", "member");
        var profile = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        (await client.PutAsJsonAsync("/api/auth/profile", new ProfileInput { DisplayName = "Lunchvän", AvatarDataUrl = TestProfileImage.Png, Revision = profile.ProfileRevision })).EnsureSuccessStatusCode();
        var input = new OrderInput { MenuItemId = page.Dishes.Single(d => d.Name == "Tisdagssoppa").Id };
        (await client.PostAsJsonAsync("/api/restaurants/2/orders", input)).EnsureSuccessStatusCode();
        var order = Assert.Single((await client.GetFromJsonAsync<DailyOrderList>("/api/restaurants/2/orders"))!.Orders);
        Assert.Equal("Lunchvän", order.Name);
        Assert.Equal(TestProfileImage.Png, order.AvatarDataUrl);
        input.MenuItemId = page.Dishes.Single(d => d.Name == "Nästa veckas fisk").Id;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/restaurants/2/orders", input)).StatusCode);
        input.MenuItemId = 5; // Legacy example dishes must not bypass the published menu.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/restaurants/2/orders", input)).StatusCode);
    }

    private sealed class TestApp(bool locked = false) : WebApplicationFactory<Program>
    {
        public new HttpClient CreateClient()
        {
            var client = base.CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", "admin");
            return client;
        }
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.Configure<SupabaseOptions>(o => { o.Url = "https://auth.example.test"; o.PublishableKey = "sb_publishable_test"; });
                services.AddHttpClient("SupabaseVerification").ConfigurePrimaryHttpMessageHandler(() => new AuthServer());
                services.RemoveAll<DbContextOptions<PizzaDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PizzaDbContext>>();
                services.RemoveAll<TimeProvider>();
                connection.Open();
                services.AddDbContext<PizzaDbContext>(o => o.UseSqlite(connection));
                services.AddSingleton<TimeProvider>(new FixedClock());
                services.Configure<OrderingOptions>(o => o.LockAfterDeadline = locked);
            });
        }
        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PizzaDbContext>().Database.EnsureCreated();
            var db = scope.ServiceProvider.GetRequiredService<PizzaDbContext>();
            foreach (var id in new[] { new TestUser().Id, Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("33333333-3333-3333-3333-333333333333") })
                db.UserProfiles.Add(new() { UserId = id, DisplayName = id == new TestUser().Id ? "AnnaMember" : id.ToString().StartsWith("222") ? "Other" : "Anna", Revision = Guid.NewGuid() });
            db.SaveChanges();
            return host;
        }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-09-22T12:00:00Z");
    }

    private sealed class AuthServer : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = request.Headers.Authorization?.Parameter;
            if (token is not ("admin" or "member" or "other" or "forged-admin" or "unapproved" or "new-member"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var role = token == "admin" ? "admin" : token == "unapproved" ? null : "member";
            var id = token == "new-member" ? Guid.Parse("44444444-4444-4444-4444-444444444444")
                : token == "admin" ? Guid.Parse("33333333-3333-3333-3333-333333333333")
                : token == "other" ? Guid.Parse("22222222-2222-2222-2222-222222222222") : new TestUser().Id;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id, email = "person@example.test", email_confirmed_at = "2026-09-01T12:00:00Z",
                    app_metadata = new { pizza_role = role }, user_metadata = new { pizza_role = "admin" }, is_anonymous = false
                })
            });
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-token")]
    [InlineData("expired-token")]
    [InlineData("unapproved")]
    public async Task Anonymous_invalid_and_unapproved_users_cannot_read_or_write(string? token)
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = token == null ? null : new("Bearer", token);
        foreach (var path in new[] { "/api/restaurants", "/api/restaurants/1/menu", "/api/restaurants/1/orders", "/api/auth/me" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/restaurants/1/orders", Pizza())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/restaurants/1/orders/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/auth/profile", new ProfileInput { DisplayName = "Nick" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/config")).StatusCode);
    }

    [Fact]
    public async Task Member_cannot_impersonate_owner_edit_others_or_promote_self_and_admin_can_manage_orders()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "member");
        var forged = new { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Coca-Cola 33cl", Quantity = 1, Name = "Anna",
            OwnerUserId = Guid.NewGuid(), IsAdmin = true };
        var created = await client.PostAsJsonAsync("/api/restaurants/1/orders", forged);
        created.EnsureSuccessStatusCode();
        var order = (await created.Content.ReadFromJsonAsync<OrderDetails>())!;
        Assert.Equal(new TestUser().Id, order.OwnerUserId);
        var input = Pizza(); input.Revision = order.Revision;
        var ownEdit = await client.PutAsJsonAsync($"/api/restaurants/1/orders/{order.Id}", input);
        ownEdit.EnsureSuccessStatusCode();
        order = (await ownEdit.Content.ReadFromJsonAsync<OrderDetails>())!;
        input.Revision = order.Revision;
        client.DefaultRequestHeaders.Authorization = new("Bearer", "other");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/restaurants/1/orders/{order.Id}", input)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync($"/api/restaurants/1/orders/{order.Id}?revision={order.Revision}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/restaurants/1/orders/{order.Id}/collector", new CollectorInput(true, order.Revision, order.OrderDate))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "forged-admin");
        Assert.False((await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.IsAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/restaurants/1/orders/complete", new CompleteDayInput(order.OrderDate, []))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "admin");
        Assert.NotEqual(order.OwnerUserId, (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/restaurants/1/orders/{order.Id}", input)).StatusCode);
    }

    [Fact]
    public async Task First_login_requires_profile_then_orders_use_persisted_alias_and_changes_preserve_old_orders()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "new-member");
        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.Equal("", user.DisplayName);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/restaurants/1/orders", Pizza())).StatusCode);
        var response = await client.PutAsJsonAsync("/api/auth/profile", new { DisplayName = "  Pizzakungen  ", AvatarDataUrl = TestProfileImage.Png,
            Revision = Guid.Empty, UserId = new TestUser().Id, IsAdmin = true });
        response.EnsureSuccessStatusCode();
        user = (await response.Content.ReadFromJsonAsync<SignedInUser>())!;
        Assert.Equal("Pizzakungen", user.DisplayName);
        Assert.Equal(TestProfileImage.Png, user.AvatarDataUrl);
        Assert.False(user.IsAdmin);
        var input = Pizza(); input.Name = "Någon annan";
        var created = await client.PostAsJsonAsync("/api/restaurants/1/orders", input);
        created.EnsureSuccessStatusCode();
        var first = (await created.Content.ReadFromJsonAsync<OrderDetails>())!;
        Assert.Equal("Pizzakungen", first.Name);
        var savedProfile = new ProfileInput { DisplayName = "Nytt nick", Revision = user.ProfileRevision, AvatarDataUrl = null };
        (await client.PutAsJsonAsync("/api/auth/profile", savedProfile)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/auth/profile", savedProfile)).StatusCode);
        var persisted = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.Equal("Nytt nick", persisted.DisplayName);
        Assert.Null(persisted.AvatarDataUrl);
        var next = await client.PostAsJsonAsync("/api/restaurants/1/orders", input);
        Assert.Equal("Nytt nick", (await next.Content.ReadFromJsonAsync<OrderDetails>())!.Name);
        input.Revision = first.Revision;
        var edited = await client.PutAsJsonAsync($"/api/restaurants/1/orders/{first.Id}", input);
        Assert.Equal("Pizzakungen", (await edited.Content.ReadFromJsonAsync<OrderDetails>())!.Name);
        client.DefaultRequestHeaders.Authorization = new("Bearer", "member");
        Assert.Equal("AnnaMember", (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ett\nnamn")]
    public async Task Blank_or_multiline_nickname_is_rejected(string name)
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/profile",
            new ProfileInput { DisplayName = name, Revision = user.ProfileRevision })).StatusCode);
        Assert.Equal("Anna", (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.DisplayName);
    }

    [Fact]
    public async Task Invalid_images_and_too_long_names_are_rejected_without_modifying_profile()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        var user = (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!;
        foreach (var image in new[] { "https://example.test/tracking.png", "data:image/svg+xml,<svg></svg>", "data:image/png;base64,broken", new string('x', ProfileImages.MaxDataUrlLength + 1) })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/profile",
                new ProfileInput { DisplayName = "Nick", AvatarDataUrl = image, Revision = user.ProfileRevision })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/auth/profile",
            new ProfileInput { DisplayName = new string('x', 101), Revision = user.ProfileRevision })).StatusCode);
        Assert.Equal("Anna", (await client.GetFromJsonAsync<SignedInUser>("/api/auth/me"))!.DisplayName);
    }
}
