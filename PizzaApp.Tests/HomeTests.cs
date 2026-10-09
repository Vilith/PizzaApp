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
    private readonly Bunit.JSRuntimeInvocationHandler scrollHandler;
    public HomeTests()
    {
        scrollHandler = JSInterop.SetupVoid("pizzaPage.center", _ => true);
        scrollHandler.SetVoidResult();
        JSInterop.SetupVoid("pizzaEntrance.play").SetVoidResult();
        JSInterop.SetupVoid("pizzaEntrance.play", "Pizza").SetVoidResult();
        JSInterop.SetupVoid("pizzaEntrance.play", "Sperring").SetVoidResult();
        Services.AddSingleton<ISperringApiService>(new FakeSperring());
        Services.AddSingleton<IAuthService>(auth);
        Services.AddSingleton<IOrderApiService>(orders);
        Services.AddSingleton<IRestaurantApiService>(new FakeRestaurants());
        Services.AddSingleton<IStatisticsApiService>(new FakeStatistics());
    }

    [Fact]
    public void Landing_page_has_two_choices_and_no_order_list()
    {
        var page = RenderComponent<Home>();
        Assert.Equal(2, page.FindAll(".restaurant-card").Count);
        Assert.Contains("Pizza", page.Find("[data-restaurant='1']").TextContent);
        Assert.Contains("Sperring", page.Find("[data-restaurant='2']").TextContent);
        Assert.Empty(page.FindAll("#daily-list, .menu-list"));
        Assert.Equal("🍕 Kika på lite statistik -->", page.Find(".statistics-teaser").TextContent);
        Assert.Equal("/statistics", page.Find(".statistics-teaser").GetAttribute("href"));
        page.Find("[data-restaurant='1']").Click();
        Assert.Contains(JSInterop.Invocations, call => call.Identifier == "pizzaEntrance.play" && Equals(call.Arguments[0], "Pizza"));
        Assert.EndsWith("/pizzerian", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/");
        Assert.Equal(2, page.FindAll(".restaurant-card").Count);
        Assert.Empty(page.FindAll("#daily-list, .menu-list"));
    }

    [Theory]
    [InlineData("/pizzerian", null, "Vesuvio", "Dagens rätt")]
    [InlineData("/sperring", "Veckans goda.", "Meny vecka", "Vesuvio")]
    public void Direct_address_loads_only_the_selected_restaurant(string path, string? title, string dish, string otherDish)
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo(path);
        var page = RenderComponent<Home>();
        if (title == null) Assert.Empty(page.FindAll("h1"));
        else Assert.Equal(title, page.Find("h1").TextContent);
        Assert.Contains(dish, page.Markup);
        Assert.DoesNotContain(otherDish, page.Markup);
        Assert.Empty(page.FindAll(".restaurant-card"));
    }

    [Fact]
    public void Navigating_between_restaurant_addresses_clears_the_previous_selection()
    {
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/pizzerian");
        var page = RenderComponent<Home>();
        page.Find("[data-menu-item='2']").Click();
        navigation.NavigateTo("/sperring");
        Assert.Contains("Veckans goda.", page.Find("h1").TextContent);
        Assert.Empty(page.FindAll("#sauce, #drink, form"));
        Assert.DoesNotContain("Vesuvio", page.Markup);
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
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/");
        page.Find("[data-restaurant='2']").Click();
        page.WaitForAssertion(() => Assert.Contains("Meny vecka", page.Markup));
        Assert.DoesNotContain("Vesuvio", page.Markup);
        Assert.Empty(page.FindAll("[data-menu-item]"));
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
        page.Find("#drink").Change("Coca-Cola 33cl");
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
        page.Find("#drink").Change("Coca-Cola 33cl");
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
    public void Sperring_has_two_week_menus_and_returns_to_doors()
    {
        var page = RenderComponent<Home>();
        page.Find("[data-restaurant='2']").Click();
        Assert.Equal(2, page.FindAll(".week-card").Count);
        Assert.Single(page.FindAll("#daily-list"));
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/");
        Assert.Equal(2, page.FindAll(".restaurant-card").Count);
        page.Find("[data-restaurant='1']").Click();
        Assert.NotEmpty(page.FindAll("[data-menu-item='2']"));
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
        Assert.Contains("3 × Coca-Cola 33cl", page.Find("[data-list='drinks']").TextContent);
        Assert.DoesNotContain("Vesuvio", page.Find(".summary-box").TextContent);
        Assert.True(page.Find("[data-action='complete']").HasAttribute("disabled"));
        page.Find("[data-collector='1']").Change(true);
        page.Find("[data-action='complete']").Click();
        page.WaitForAssertion(() => Assert.Contains("Hämtad och sparad i historiken", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list .summary-box, #daily-list details, [data-action='complete']"));
        Assert.Empty(page.FindAll(".list-link"));
        Assert.DoesNotContain("Ingen har beställt ännu", page.Markup);
        page.Find("#daily-list .btn-outline-primary").Click();
        page.WaitForAssertion(() => Assert.Contains("Dagens lista är rensad", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list details"));
        // Returning to the restaurant reads persisted completion, not local UI state.
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/");
        page.Find("[data-restaurant='1']").Click();
        page.WaitForAssertion(() => Assert.Contains("Dagens lista är rensad", page.Markup));
        Assert.Empty(page.FindAll("#daily-list .order-row, #daily-list details"));
    }

    [Fact]
    public void Signed_out_users_only_see_login_and_logout_clears_loaded_orders()
    {
        auth.User = null;
        var layout = RenderComponent<PizzaApp.Components.Layout.MainLayout>();
        var page = RenderComponent<Home>();
        Assert.NotEmpty(page.FindAll("#login-alias"));
        Assert.Empty(page.FindAll("[data-restaurant]"));
        page.Find("#login-alias").Change("Anna");
        page.Find("#login-password").Change("test-password");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[data-restaurant='1']")));
        page.Find("[data-restaurant='1']").Click();
        layout.Find("[data-action='logout']").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("#login-alias")));
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
        var layout = RenderComponent<PizzaApp.Components.Layout.MainLayout>();
        var page = RenderComponent<Home>();
        Assert.NotEmpty(page.FindAll("#profile-name"));
        Assert.Empty(page.FindAll("[data-restaurant]"));
        page.Find("#profile-name").Change("Pizzafan");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("[data-restaurant='1']")));
        Assert.Contains("Pizzafan", layout.Find(".profile-identity").TextContent);
        page.Find("[data-restaurant='1']").Click();
        page.Find("[data-menu-item='2']").Click();
        Assert.Contains("Pizzafan", page.Find(".order-profile-name").TextContent);
        Assert.Empty(page.FindAll("#customer-name"));
        Assert.Equal("/settings", layout.Find("[data-action='settings']").GetAttribute("href"));
    }

    [Fact]
    public void Pizzeria_groups_menu_and_new_dishes_can_be_selected()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/pizzerian");
        var page = RenderComponent<Home>();
        Assert.Equal(new[] { "Pizzor 1–12", "Sallader", "Kebab", "Stekrätter" },
            page.FindAll("[role=tab]").Select(h => h.TextContent));
        Assert.Contains("Tomat och ost", page.Find(".menu-category").TextContent);
        Assert.Single(page.FindAll(".menu-number"));
        page.Find("#tab-Stekrätter").Click();
        Assert.Empty(page.FindAll("[data-menu-item=2]"));
        Assert.Contains("Med bröd & Pommes", page.Find("[data-menu-item='65']").TextContent);
        page.Find("[data-menu-item='65']").Click();
        Assert.Equal("Hamburgare 90gr", page.Find("#selection-heading").TextContent);
    }

    [Fact]
    public void Selection_defaults_sauce_centers_form_and_has_no_redundant_list_button()
    {
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/pizzerian");
        var page = RenderComponent<Home>();
        page.Find("[data-menu-item='2']").Click();
        Assert.Equal("Ingen sås", page.Find("#sauce").GetAttribute("value"));
        Assert.Equal(PizzaChoices.Drinks, page.FindAll("#drink option").Skip(1).Select(o => o.TextContent));
        Assert.Contains(JSInterop.Invocations, call => call.Identifier == "pizzaPage.center" && Equals(call.Arguments[0], "selection-panel"));
        Assert.Empty(page.FindAll(".list-link"));
        Assert.EndsWith("/pizzerian", navigation.Uri);
    }

    [Theory]
    [InlineData("Skinka", "Ingen sås")]
    [InlineData("Fläskfilé, Bearnaisesås (Inbakad)", "Bearnaisesås")]
    [InlineData("Köttfärs, Vitlökssås", "Vitlökssås")]
    [InlineData("Kebabkött, Kebabsås", "Kebabsås")]
    [InlineData("Kebabkött, Stark kebabsås, Vitlökssås", "Kebabsås (Stark)")]
    [InlineData("Kebabsås (Mixad)", "Kebabsås (Mixad)")]
    [InlineData("Tacosås, Vitlökssås", "Vitlökssås")]
    public void Ingredient_sauce_is_selected(string ingredients, string expected)
        => Assert.Equal(expected, MenuSauce.DefaultFor(ingredients));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scroll_failure_does_not_prevent_selecting_and_saving(bool timeout)
    {
        scrollHandler.SetException<Exception>(timeout
            ? new TaskCanceledException("Scroll timed out")
            : new Microsoft.JSInterop.JSException("pizzaPage.center is undefined"));
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/pizzerian");
        var page = RenderComponent<Home>();
        page.Find("[data-menu-item='2']").Click();
        page.WaitForAssertion(() => Assert.Equal("Vesuvio", page.Find("#selection-heading").TextContent));
        page.Find("#drink").Change("Fanta 33cl");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotNull(orders.Saved));
        Assert.Equal(2, orders.Saved!.MenuItemId);
        Assert.Equal("Ingen sås", orders.Saved.Sauce);
    }

    private class FakeSperring : ISperringApiService
    {
        public Task<SperringPage> GetAsync() => Task.FromResult(new SperringPage(2,
            [new() { Slot = 1, Year = 2026, Week = 39 }, new() { Slot = 2, Year = 2026, Week = 40 }], []));
        public Task SaveAsync(SperringMenu menu) => Task.CompletedTask;
    }

    [Fact]
    public void Statistics_page_shows_rankings_and_changes_period()
    {
        var fake = new FakeStatistics();
        Services.AddSingleton<IStatisticsApiService>(fake);
        var page = RenderComponent<Statistics>();
        Assert.Contains("Totalt beställningsvärde", page.Markup);
        Assert.DoesNotContain("portioner × 95 kr", page.Markup);
        Assert.Contains("Pantvärde", page.Markup);
        Assert.Contains("100 kr", page.Find(".stat-card:last-child").TextContent);
        Assert.Contains("Topp 5 beställda pizzor", page.Markup);
        Assert.Contains("Dryckernas topplista", page.Markup);
        Assert.Contains("De där som hämtar", page.Markup);
        Assert.Equal(3, page.FindAll(".ranking").Count);
        page.Find("#statistics-period").Change("2026");
        page.WaitForAssertion(() => Assert.Equal(2026, fake.LastYear));
        Assert.Equal("2026", page.Find("#statistics-period").GetAttribute("value"));
    }

    [Fact]
    public void Statistics_page_offers_retry_when_connection_fails()
    {
        var fake = new FakeStatistics { Fail = true };
        Services.AddSingleton<IStatisticsApiService>(fake);
        var page = RenderComponent<Statistics>();
        Assert.Contains("Kunde inte hämta statistiken", page.Find("[role='alert']").TextContent);
        fake.Fail = false;
        page.Find("button").Click();
        page.WaitForAssertion(() => Assert.Equal(4, page.FindAll(".stat-card").Count));
    }

    [Fact]
    public void Standalone_drink_has_no_sauce_and_saves_quantity_and_can_switch_to_food()
    {
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/pizzerian");
        var page = RenderComponent<Home>();
        page.Find("[data-action='drink-only']").Click();
        Assert.Contains("15 kr per burk", page.Find("#selection-heading").TextContent);
        Assert.Empty(page.FindAll("#sauce"));
        page.Find("#drink").Change("Fanta 33cl");
        page.Find("#quantity").Change("2");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.True(orders.Saved?.DrinkOnly));
        Assert.Equal(2, orders.Saved!.Quantity);
        Assert.Equal(0, orders.Saved.MenuItemId);
        Assert.Null(orders.Saved.Sauce);
        page.Find("[data-action='drink-only']").Click();
        page.Find("[data-menu-item='2']").Click();
        Assert.Single(page.FindAll("#sauce"));
        page.Find("#drink").Change("Fanta 33cl");
        page.Find("form").Submit();
        Assert.False(orders.Saved.DrinkOnly);
    }

    private class FakeStatistics : IStatisticsApiService
    {
        public bool Fail { get; set; }
        public int? LastYear { get; private set; }
        public Task<PizzeriaStatistics> GetAsync(int? year = null)
        {
            LastYear = year;
            if (Fail) throw new HttpRequestException("Offline");
            return Task.FromResult(new PizzeriaStatistics
            {
                Year = year, StartDate = new(2026, 10, 2), AvailableYears = [2026], TotalPizzas = 42, TotalMeals = 50, TotalDrinks = 50, CompletedDays = 4,
                TopPizzas = [new("Vesuvio", 42)], Drinks = [new("Cola", 50)], Collectors = [new("Anna", 4)]
            });
        }
    }

    private class FakeRestaurants : IRestaurantApiService
    {
        public Task<List<Restaurant>> GetRestaurantsAsync() => Task.FromResult<List<Restaurant>>([
            new() { Id = 1, Name = "Kvänum Pizzeria", IsPizzeria = true }, new() { Id = 2, Name = "Sperring" }]);
        public Task<List<MenuItem>> GetMenuAsync(int id) => Task.FromResult<List<MenuItem>>(id == 1
            ? [
                new() { Id = 2, RestaurantId = 1, Name = "Vesuvio", MenuNumber = 2, Category = "Pizzor", Price = 85 },
                new() { Id = 48, RestaurantId = 1, Name = "Amerikansk Sallad", Category = "Sallader", Description = "Skinka, Räkor", Price = 90 },
                new() { Id = 56, RestaurantId = 1, Name = "Kebab m. Bröd", Category = "Kebab", Price = 90 },
                new() { Id = 65, RestaurantId = 1, Name = "Hamburgare 90gr", Category = "Stekrätter", Description = "Med bröd & Pommes", Price = 80 }]
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
        public OrderDetails Existing = new() { Id = 1, RestaurantId = 1, MenuItemId = 2, Pizza = "Vesuvio", Sauce = "Ingen sås", Drink = "Coca-Cola 33cl", Revision = Guid.NewGuid() };
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
