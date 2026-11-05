// Regressionstester för ProfileSettings: förutsättningar, testfall och testdubblar.

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PizzaApp.Components.Pages;
using PizzaApp.Services;
using PizzaApp.Shared;

namespace PizzaApp.Tests;

public class ProfileSettingsTests : TestContext
{
    private readonly FakeAuth auth = new();
    public ProfileSettingsTests()
    {
        Services.AddSingleton<IAuthService>(auth);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Admin_can_toggle_member_preview_and_return_to_admin_view()
    {
        var page = RenderComponent<ProfileSettings>();
        page.Find("#view-as-member").Change(true);
        Assert.True(auth.ViewAsMember);
        Assert.False(((IAuthService)auth).ShowAdminControls);
        Assert.True(auth.User!.IsAdmin);
        Assert.Single(page.FindAll("#view-as-member"));
        page.Find("#view-as-member").Change(false);
        Assert.True(((IAuthService)auth).ShowAdminControls);
    }

    [Fact]
    public void Members_do_not_see_preview_setting()
    {
        auth.User = auth.User! with { IsAdmin = false };
        var page = RenderComponent<ProfileSettings>();
        Assert.Empty(page.FindAll("#view-as-member"));
        auth.SetViewAsMember(true);
        Assert.False(auth.ViewAsMember);
    }
    // TESTFALL: beteende, behörighet och felvägar. Hjälpklasser finns efter testfallen.

    [Fact]
    public void Settings_saves_nickname_removes_photo_and_shows_confirmation()
    {
        auth.User = auth.User! with { AvatarDataUrl = TestProfileImage.Png };
        var page = RenderComponent<ProfileSettings>();
        Assert.Equal("Anna", page.Find("#profile-name").GetAttribute("value"));
        Assert.NotEmpty(page.FindAll("img"));
        page.Find("#profile-name").Change("Pizzafan");
        page.Find("[data-action='remove-photo']").Click();
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Equal("Pizzafan", auth.User!.DisplayName));
        Assert.Null(auth.User!.AvatarDataUrl);
        Assert.Contains("Din profil är sparad", page.Markup);
        Assert.Empty(page.FindAll("img"));
    }

    [Fact]
    public void Settings_requires_nickname_and_signed_out_users_see_login()
    {
        auth.User = auth.User! with { DisplayName = "" };
        var layout = RenderComponent<PizzaApp.Components.Layout.MainLayout>();
        var page = RenderComponent<ProfileSettings>();
        page.Find("form").Submit();
        Assert.Contains("Ange ditt namn eller nick", page.Markup);
        Assert.Equal("", auth.User!.DisplayName);
        layout.Find("[data-action='logout']").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("#login-alias")));
        Assert.Empty(page.FindAll("#profile-name"));
    }

    [Fact]
    public void Image_validation_rejects_oversized_dimensions_and_truncated_data()
    {
        Assert.True(ProfileImages.IsValid(TestProfileImage.Png));
        var bytes = Convert.FromBase64String(TestProfileImage.Png[ProfileImages.Prefix.Length..]);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), 10000);
        Assert.False(ProfileImages.IsValid(ProfileImages.Prefix + Convert.ToBase64String(bytes)));
        Assert.False(ProfileImages.IsValid(TestProfileImage.Png[..^8]));
    }
}

public static class TestProfileImage
{
    public const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF9sAAAAASUVORK5CYII=";
}
