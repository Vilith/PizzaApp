using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PizzaApp.Components.Pages;
using PizzaApp.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class SperringUiTests : TestContext
{
    private readonly FakeAuth auth = new();
    private readonly MenuApi menus = new();
    private readonly OrderApi orders = new();
    public SperringUiTests()
    {
        JSInterop.SetupVoid("pizzaPage.center", "sperring-selection").SetVoidResult();
        Services.AddSingleton<IAuthService>(auth);
        Services.AddSingleton<ISperringApiService>(menus);
        Services.AddSingleton<IOrderApiService>(orders);
    }

    [Fact]
    public void Member_sees_two_weeks_alias_avatar_and_can_choose_future_dishes()
    {
        auth.User = auth.User! with { IsAdmin = false };
        var page = RenderComponent<SperringMenuPage>();
        Assert.Equal(2, page.FindAll(".week-card").Count);
        Assert.Equal(10, page.FindAll(".day-row").Count);
        Assert.DoesNotContain("Redigera meny", page.Markup);
        Assert.Empty(page.FindAll(".regular-editor"));
        Assert.Equal(5, page.FindAll(".day-tabs button").Count);
        Assert.Contains("Alias", page.Find(".order-head").TextContent);
        Assert.Contains("Lunchvän", page.Find(".person").TextContent);
        Assert.Equal(TestProfileImage.Png, page.Find(".person img").GetAttribute("src"));
        page.FindAll("button.dish").Single(b => b.TextContent.Contains("Soppa")).Click();
        page.Find(".selection .primary").Click();
        Assert.Equal(12, orders.Saved!.MenuItemId);
        Assert.Equal(new DateOnly(2026, 9, 22), orders.Saved.DeliveryDate);
        Assert.Contains("Din beställning är sparad.", page.Markup);
    }

    [Fact]
    public void Admin_edits_a_copy_and_cancel_preserves_published_menu()
    {
        var page = RenderComponent<SperringMenuPage>();
        page.Find(".week-heading button").Click();
        page.Find(".editor textarea").Change("Ny rätt");
        Assert.Equal("Pasta", menus.Page.Menus[0].RegularDishes);
        page.FindAll(".editor button").Single(b => b.TextContent == "Avbryt").Click();
        Assert.Empty(page.FindAll(".editor textarea"));
        Assert.DoesNotContain("Ny rätt", page.Markup);
        page.Find(".week-heading button").Click();
        page.Find(".editor textarea").Change("Ny rätt");
        page.Find(".editor .primary").Click();
        Assert.Equal("Ny rätt", menus.Saved!.Days[0]);
    }

    [Fact]
    public void Future_dish_uses_its_date_and_opens_that_days_orders()
    {
        var page = RenderComponent<SperringMenuPage>();
        page.FindAll("button.dish").Single(b => b.TextContent.Contains("Fisk")).Click();
        page.Find(".selection .primary").Click();
        Assert.Equal(new DateOnly(2026, 9, 23), orders.Saved!.DeliveryDate);
        Assert.Contains("2026-09-23", page.Find(".orders .eyebrow").TextContent);
        Assert.Contains("0 av minst 5", page.Markup);
    }

    [Fact]
    public void Enter_adds_and_focuses_a_blank_row_and_saves_each_dish_once()
    {
        JSInterop.SetupVoid("pizzaPage.focus", _ => true).SetVoidResult();
        var page = RenderComponent<SperringMenuPage>();
        var first = page.Find(".regular-dish-row input");
        first.Input("Kycklingsallad");
        first.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(2, page.FindAll(".regular-dish-row input").Count);
        var next = page.FindAll(".regular-dish-row input").ToArray()[1];
        Assert.Equal("", next.GetAttribute("value"));
        Assert.Equal(next.Id, JSInterop.Invocations["pizzaPage.focus"].Single().Arguments[0]);
        next.Input("Vegetarisk pasta");
        page.FindAll(".regular-dish-row input").ToArray()[1].KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(3, page.FindAll(".regular-dish-row input").Count);
        page.Find(".regular-editor .primary").Click();
        Assert.Equal("Kycklingsallad\nVegetarisk pasta", menus.SavedRegular!.Dishes);
    }

    [Fact]
    public void Daily_dish_has_its_own_group_above_regular_dishes()
    {
        var description = "Schnitzel, serveras med klyftpotatis, ärtor, vinbärsgelé och ruccolasallad";
        menus.Page.Menus[0].Days[1] = description;
        menus.Page.Dishes.Add(new(15, description));
        var page = RenderComponent<SperringMenuPage>();
        var day = page.FindAll(".day-row").ToArray()[1];
        Assert.Contains(description, day.QuerySelector(".daily-dishes")!.TextContent);
        Assert.DoesNotContain("Pasta", day.QuerySelector(".daily-dishes")!.TextContent);
        Assert.Contains("Pasta", day.QuerySelector(".regular-dishes")!.TextContent);
        Assert.Equal("daily-dishes", day.QuerySelector(".dish-group")!.ClassList.Last());
        day.QuerySelector(".daily-dishes button")!.Click();
        page.Find(".selection .primary").Click();
        Assert.Equal(15, orders.Saved!.MenuItemId);
    }

    [Fact]
    public void Examples_stay_in_the_editor_until_saved()
    {
        var page = RenderComponent<SperringMenuPage>();
        page.Find(".week-heading button").Click();
        page.FindAll(".editor button").Single(b => b.TextContent == "Fyll i exempelrätter").Click();
        Assert.Equal("Pasta", menus.Page.Menus[0].RegularDishes);
        Assert.Contains("Köttbullar", page.Find(".editor textarea").GetAttribute("value") ?? page.Find(".editor textarea").TextContent);
    }

    private class MenuApi : ISperringApiService
    {
        public SperringMenu? Saved;
        public SperringRegularInput? SavedRegular;
        public SperringPage Page = new(2,
            [new() { Slot = 1, Year = 2026, Week = 39, RegularDishes = "Pasta", Days = ["", "Soppa", "Fisk", "", ""] },
             new() { Slot = 2, Year = 2026, Week = 40, RegularDishes = "Nästa vecka" }],
            [new(11, "Pasta"), new(12, "Soppa"), new(13, "Fisk"), new(14, "Nästa vecka")]);
        public Task<SperringPage> GetAsync() => Task.FromResult(Page);
        public Task SaveAsync(SperringMenu menu) { Saved = menu; return Task.CompletedTask; }
        public Task SaveRegularAsync(SperringRegularInput input) { SavedRegular = input; return Task.CompletedTask; }
    }
    private class OrderApi : IOrderApiService
    {
        public OrderInput? Saved;
        public Task<DailyOrderList> GetTodayAsync(int id) => Task.FromResult(new DailyOrderList
        { Date = new(2026, 9, 22), Orders = [new() { Id = 1, Name = "Lunchvän", Pizza = "Pasta", AvatarDataUrl = TestProfileImage.Png }] });
        public async Task<DailyOrderList> GetDayAsync(int id, DateOnly date)
        {
            var day = await GetTodayAsync(id);
            if (day.Date != date) { day.Date = date; day.Orders = []; }
            return day;
        }
        public Task<OrderDetails> SaveAsync(int id, OrderInput input, int? orderId = null)
        { Saved = input; return Task.FromResult(new OrderDetails()); }
        public Task DeleteAsync(int restaurantId, int id, Guid revision) => Task.CompletedTask;
        public Task<DailyOrderList> SetCollectorAsync(int restaurantId, int id, CollectorInput input) => GetTodayAsync(restaurantId);
        public Task<DailyOrderList> CompleteAsync(int restaurantId, CompleteDayInput input) => GetTodayAsync(restaurantId);
    }
}
