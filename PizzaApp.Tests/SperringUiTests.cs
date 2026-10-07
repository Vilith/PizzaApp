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
        Services.AddSingleton<IAuthService>(auth);
        Services.AddSingleton<ISperringApiService>(menus);
        Services.AddSingleton<IOrderApiService>(orders);
    }

    [Fact]
    public void Member_sees_two_weeks_alias_avatar_and_can_only_choose_todays_dishes()
    {
        auth.User = auth.User! with { IsAdmin = false };
        var page = RenderComponent<SperringMenuPage>();
        Assert.Equal(2, page.FindAll(".week-card").Count);
        Assert.Equal(10, page.FindAll(".day-row").Count);
        Assert.DoesNotContain("Redigera meny", page.Markup);
        Assert.Equal(2, page.FindAll("button.dish").Count);
        Assert.Contains("Alias", page.Find(".order-head").TextContent);
        Assert.Contains("Lunchvän", page.Find(".person").TextContent);
        Assert.Equal(TestProfileImage.Png, page.Find(".person img").GetAttribute("src"));
        page.FindAll("button.dish").Single(b => b.TextContent.Contains("Soppa")).Click();
        page.Find(".selection .primary").Click();
        Assert.Equal(12, orders.Saved!.MenuItemId);
        Assert.Contains("Din beställning är sparad.", page.Markup);
    }

    [Fact]
    public void Admin_edits_a_copy_and_cancel_preserves_published_menu()
    {
        var page = RenderComponent<SperringMenuPage>();
        page.Find(".week-heading button").Click();
        page.Find("textarea").Change("Ny rätt");
        Assert.Equal("Pasta", menus.Page.Menus[0].RegularDishes);
        page.FindAll(".editor button").Single(b => b.TextContent == "Avbryt").Click();
        Assert.Empty(page.FindAll("textarea"));
        Assert.DoesNotContain("Ny rätt", page.Markup);
        page.Find(".week-heading button").Click();
        page.Find("textarea").Change("Ny rätt");
        page.Find(".editor .primary").Click();
        Assert.Equal("Ny rätt", menus.Saved!.RegularDishes);
    }

    private class MenuApi : ISperringApiService
    {
        public SperringMenu? Saved;
        public SperringPage Page = new(2,
            [new() { Slot = 1, Year = 2026, Week = 39, RegularDishes = "Pasta", Days = ["", "Soppa", "Fisk", "", ""] },
             new() { Slot = 2, Year = 2026, Week = 40, RegularDishes = "Nästa vecka" }],
            [new(11, "Pasta"), new(12, "Soppa"), new(13, "Fisk"), new(14, "Nästa vecka")]);
        public Task<SperringPage> GetAsync() => Task.FromResult(Page);
        public Task SaveAsync(SperringMenu menu) { Saved = menu; return Task.CompletedTask; }
    }
    private class OrderApi : IOrderApiService
    {
        public OrderInput? Saved;
        public Task<DailyOrderList> GetTodayAsync(int id) => Task.FromResult(new DailyOrderList
        { Date = new(2026, 9, 22), Orders = [new() { Id = 1, Name = "Lunchvän", Pizza = "Pasta", AvatarDataUrl = TestProfileImage.Png }] });
        public Task<OrderDetails> SaveAsync(int id, OrderInput input, int? orderId = null)
        { Saved = input; return Task.FromResult(new OrderDetails()); }
        public Task DeleteAsync(int restaurantId, int id, Guid revision) => Task.CompletedTask;
        public Task<DailyOrderList> SetCollectorAsync(int restaurantId, int id, CollectorInput input) => GetTodayAsync(restaurantId);
        public Task<DailyOrderList> CompleteAsync(int restaurantId, CompleteDayInput input) => GetTodayAsync(restaurantId);
    }
}
