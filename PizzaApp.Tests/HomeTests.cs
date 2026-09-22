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
    public HomeTests()
    {
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
    public void Saves_pizza_without_name_and_refreshes_shared_list()
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
        Assert.Equal("", orders.Saved.Name);
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
        public bool Locked;
        public bool FailSave;
        public OrderInput? Saved;
        public int SavedRestaurant;
        public int? UpdatedId;
        public (int, int, Guid)? Deleted;
        public OrderDetails Existing = new() { Id = 1, RestaurantId = 1, MenuItemId = 2, Pizza = "Vesuvio", Sauce = "Ingen sås", Drink = "Vatten", Revision = Guid.NewGuid() };
        public Task<DailyOrderList> GetTodayAsync(int id) => Task.FromResult(new DailyOrderList
        { Date = new(2026, 9, 22), IsPizzeria = id == 1, IsLocked = Locked, Orders = id == 1 ? [Existing] : [] });
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
