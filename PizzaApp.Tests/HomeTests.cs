using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PizzaApp.Components.Pages;
using PizzaApp.Models;
using PizzaApp.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class HomeTests : TestContext
{
    private readonly FakeOrders orders = new();
    private readonly FakeAuth auth = new();
    public HomeTests()
    {
        Services.AddSingleton<IAuthService>(auth);
        Services.AddSingleton<IOrderApiService>(orders);
        Services.AddSingleton<IRestaurantApiService>(new FakeRestaurants());
    }

    [Fact]
    public void Restaurant_selection_shows_separate_lists_and_only_pizza_has_extras()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.WaitForAssertion(() => Assert.Contains("Vesuvio", page.Markup));
        Assert.Contains("11:15", page.Markup);
        page.Find("[data-menu-item='2']").Click();
        Assert.NotEmpty(page.FindAll("#sauce"));
        page.Find("[data-action='restaurants']").Click();
        page.Find("[data-restaurant='2']").Click();
        page.WaitForAssertion(() => Assert.Contains("Dagens rätt", page.Markup));
        Assert.DoesNotContain("Vesuvio", page.Markup);
        page.Find("[data-menu-item='100']").Click();
        Assert.Empty(page.FindAll("#sauce"));
        Assert.Empty(page.FindAll("#drink"));
    }

    [Fact]
    public void Uses_profile_name_without_retyping_and_refreshes_shared_list()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-menu-item='2']").Click();
        page.Find("#sauce").Change("Ingen sås");
        page.Find("#drink").Change("Vatten");
        page.Find("#quantity").Change("3");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotNull(orders.Saved));
        Assert.Equal(1, orders.SavedRestaurant);
        Assert.Equal(3, orders.Saved!.Quantity);
        Assert.Equal("Anna", orders.Saved.Name);
        Assert.Empty(page.FindAll("#customer-name"));
        Assert.Equal("Ingen sås", orders.Saved.Sauce);
        page.WaitForAssertion(() => Assert.Contains("Beställningen är sparad", page.Markup));
    }

    [Fact]
    public void Editing_preserves_revision_and_uses_update()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-edit='1']").Click();
        page.Find("#quantity").Change("4");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Equal(1, orders.UpdatedId));
        Assert.Equal(orders.Existing.Revision, orders.Saved!.Revision);
        Assert.Equal(4, orders.Saved.Quantity);
    }

    [Fact]
    public void Locked_list_disables_changes_and_shows_reason()
    {
        orders.Locked = true;
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.WaitForAssertion(() => Assert.Contains("låsta", page.Markup));
        Assert.True(page.Find("[data-menu-item='2']").HasAttribute("disabled"));
        Assert.True(page.Find("[data-edit='1']").HasAttribute("disabled"));
        Assert.True(page.Find("[data-delete='1']").HasAttribute("disabled"));
    }

    [Fact]
    public void Failed_save_preserves_input_and_displays_error()
    {
        orders.FailSave = true;
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-menu-item='2']").Click();
        page.Find("#sauce").Change("Ingen sås");
        page.Find("#drink").Change("Vatten");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[role='alert']")));
        Assert.NotEmpty(page.FindAll("form"));
        Assert.DoesNotContain("Beställningen är sparad", page.Markup);
    }

    [Fact]
    public void Missing_pizza_choices_cannot_be_submitted()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-menu-item='2']").Click();
        page.Find("form").Submit();
        Assert.Null(orders.Saved);
        Assert.Contains("Välj sås och dryck", page.Find("[role='alert']").TextContent);
    }

    [Fact]
    public void Ala_carte_saves_to_its_own_list_without_extras()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='2']").Click();
        page.Find("[data-menu-item='100']").Click();
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Equal(2, orders.SavedRestaurant));
        Assert.Null(orders.Saved!.Sauce);
        Assert.Null(orders.Saved.Drink);
    }

    [Fact]
    public void Delete_requires_confirmation_and_sends_current_revision()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-delete='1']").Click();
        Assert.Null(orders.Deleted);
        page.Find(".delete-confirm .btn-danger").Click();
        page.WaitForAssertion(() => Assert.Equal((1, 1, orders.Existing.Revision), orders.Deleted));
    }

    [Fact]
    public void Drinks_are_totaled_and_collecting_locks_the_day()
    {
        orders.Existing.Name = "Anna";
        orders.Existing.Quantity = 3;
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        Assert.Contains("3 × Vatten", page.Find("[data-list='drinks']").TextContent);
        Assert.DoesNotContain("Vesuvio", page.Find(".summary-box").TextContent);
        Assert.True(page.Find("[data-action='complete']").HasAttribute("disabled"));
        page.Find("[data-collector='1']").Change(true);
        page.Find("[data-action='complete']").Click();
        page.WaitForAssertion(() => Assert.Contains("Hämtad och sparad i historiken", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list .summary-box, #daily-list details, [data-action='complete']"));
        Assert.Equal("0", page.Find(".list-link span").TextContent);
        Assert.DoesNotContain("Ingen har beställt ännu", page.Markup);
        page.Find("#daily-list .btn-outline-primary").Click();
        page.WaitForAssertion(() => Assert.Contains("Dagens lista är rensad", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list details"));
        // Returning to the restaurant reads persisted completion, not local UI state.
        page.Find("[data-action='restaurants']").Click();
        page.Find("[data-restaurant='1']").Click();
        page.WaitForAssertion(() => Assert.Contains("Dagens lista är rensad", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list details"));
    }

    [Fact]
    public void Signed_out_users_only_see_login_and_logout_clears_loaded_orders()
    {
        auth.User = null;
        var page = RenderComponent<Home>();
        Assert.NotEmpty(page.FindAll("#login-email"));
        Assert.Empty(page.FindAll("[data-restaurant]"));
        page.Find("#login-email").Change("anna@example.test");
        page.Find("#login-password").Change("test-password");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[data-restaurant='1']")));
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-action='logout']").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("#login-email")));
        Assert.Empty(page.FindAll("#daily-list"));
    }

    [Fact]
    public void Members_can_only_edit_their_own_orders_and_cannot_complete_the_day()
    {
        auth.User = new(new TestUser().Id, "anna@example.test", false, "Anna");
        orders.Existing.OwnerUserId = Guid.NewGuid();
        orders.Existing.Name = "Annan person";
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='1']").Click();
        Assert.Empty(page.FindAll("[data-edit], [data-delete], [data-action='complete']"));
        Assert.True(page.Find("[data-collector='1']").HasAttribute("disabled"));
        orders.Existing.OwnerUserId = auth.User.Id;
        page.Find("#daily-list .btn-outline-primary").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[data-edit='1']")));
        Assert.False(page.Find("[data-collector='1']").HasAttribute("disabled"));
    }

    [Fact]
    public void New_account_must_choose_alias_before_ordering()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        auth.User = auth.User! with { DisplayName = "" };
        var page = RenderComponent<Home>();
        Assert.NotEmpty(page.FindAll("#profile-name"));
        Assert.Empty(page.FindAll("[data-restaurant]"));
        page.Find("#profile-name").Change("Pizzafan");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[data-restaurant='1']")));
        Assert.Contains("Pizzafan", page.Find(".profile-identity").TextContent);
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-menu-item='2']").Click();
        Assert.Contains("Pizzafan", page.Find(".order-profile-name").TextContent);
        Assert.Empty(page.FindAll("#customer-name"));
        Assert.Equal("/settings", page.Find("[data-action='settings']").GetAttribute("href"));
    }

    private class FakeRestaurants : IRestaurantApiService
    {
        public Task<List<Restaurant>> GetRestaurantsAsync() => Task.FromResult<List<Restaurant>>([
            new() { Id = 1, Name = "Kvänum Pizzeria", IsPizzeria = true }, new() { Id = 2, Name = "Sperring" }]);
        public Task<List<MenuItem>> GetMenuAsync(int id) => Task.FromResult<List<MenuItem>>(id == 1
            ? [new() { Id = 2, RestaurantId = 1, Name = "Vesuvio", Price = 95 }]
            : [new() { Id = 100, RestaurantId = 2, Name = "Dagens rätt", Price = 100 }]);
    }

    private class FakeOrders : IOrderApiService
    {
        public Task<DailyOrderList> SetCollectorAsync(int restaurantId, int id, CollectorInput input)
        { Existing.CanCollect = input.CanCollect; return GetTodayAsync(restaurantId); }
        public async Task<DailyOrderList> CompleteAsync(int restaurantId, CompleteDayInput input)
        { Completed = true; return await GetTodayAsync(restaurantId); }
        public bool Completed;
        public bool Locked;
        public bool FailSave;
        public OrderInput? Saved;
        public int SavedRestaurant;
        public int? UpdatedId;
        public (int, int, Guid)? Deleted;
        public OrderDetails Existing = new() { Id = 1, RestaurantId = 1, MenuItemId = 2, Pizza = "Vesuvio", Sauce = "Ingen sås", Drink = "Vatten", Revision = Guid.NewGuid() };
        public Task<DailyOrderList> GetTodayAsync(int id) => Task.FromResult(new DailyOrderList
        { Date = new(2026, 9, 22), IsPizzeria = id == 1, IsLocked = Locked || Completed,
          CollectedAt = Completed ? DateTime.UtcNow : null, CollectedBy = Completed ? [Existing.Name] : [],
          Orders = id == 1 ? [Existing] : [] });
        public Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null)
        {
            if (FailSave) throw new HttpRequestException("Offline");
            Saved = input; SavedRestaurant = restaurantId; UpdatedId = id;
            return Task.FromResult(Existing);
        }
        public Task DeleteAsync(int restaurantId, int id, Guid revision)
        { Deleted = (restaurantId, id, revision); return Task.CompletedTask; }
    }
}
