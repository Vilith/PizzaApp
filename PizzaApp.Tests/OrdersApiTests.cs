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
        Assert.NotEmpty(menu!);
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

    private static OrderInput Pizza() => new() { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Vatten" };

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
            if (token is not ("admin" or "member" or "other" or "forged-admin" or "unapproved"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var role = token == "admin" ? "admin" : token == "unapproved" ? null : "member";
            var id = token == "admin" ? Guid.Parse("33333333-3333-3333-3333-333333333333")
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
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/config")).StatusCode);
    }

    [Fact]
    public async Task Member_cannot_impersonate_owner_edit_others_or_promote_self_and_admin_can_manage_orders()
    {
        using var app = new TestApp(); using var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "member");
        var forged = new { MenuItemId = 2, Sauce = "Ingen sås", Drink = "Vatten", Quantity = 1, Name = "Anna",
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
}
