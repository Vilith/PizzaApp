using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PizzaApp.Api;

namespace PizzaApp.Tests;

public class WebHostingTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/pizzerian")]
    [InlineData("/sperring")]
    [InlineData("/settings")]
    public async Task Browser_routes_serve_the_app_without_requiring_a_token(string path)
    {
        using var app = new WebApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("_framework/blazor.webassembly.js", html);
        Assert.Contains("Benders dörr", html);
        Assert.DoesNotContain("blazor.webview.js", html);
    }

    [Theory]
    [InlineData("/_framework/blazor.webassembly.js")]
    [InlineData("/PizzaApp.styles.css")]
    [InlineData("/css/entrance.css")]
    [InlineData("/js/entrance.js")]
    public async Task Browser_can_load_framework_styles_and_animation(string path)
    {
        using var app = new WebApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Theory]
    [InlineData("/api/restaurants")]
    [InlineData("/api/auth/me")]
    [InlineData("/api/nonexistent")]
    public async Task Public_app_shell_does_not_make_api_routes_public(string path)
    {
        using var app = new WebApp();
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("<!DOCTYPE html>", await response.Content.ReadAsStringAsync());
    }

    private sealed class WebApp : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Load the built client's assets without loading Development user secrets.
            builder.UseEnvironment("Testing");
            builder.UseStaticWebAssets();
        }
    }
}
