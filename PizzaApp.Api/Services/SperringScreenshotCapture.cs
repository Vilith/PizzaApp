using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

// SKÄRMBILD: separat gränssnitt gör cache- och felhantering testbar utan webbläsare.
public interface ISperringScreenshotCapture
{
    Task<byte[]> CaptureAsync(CancellationToken cancellationToken);
}

public class SperringScreenshotCapture(IOptions<SperringScreenshotOptions> options) : ISperringScreenshotCapture
{
    // 1. WEBBLÄSARE: en isolerad Chromium-session besöker endast den fasta menyadressen.
    public async Task<byte[]> CaptureAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Channel = options.Value.BrowserChannel,
            Timeout = 30_000
        });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            DeviceScaleFactor = 1,
            Locale = "sv-SE",
            ReducedMotion = ReducedMotion.Reduce
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        var response = await page.GotoAsync(SperringWebsiteMenu.SourceUrl,
            new() { WaitUntil = WaitUntilState.Load, Timeout = 45_000 });
        if (response == null || !response.Ok)
            throw new InvalidDataException("Sperrings menysida kunde inte öppnas.");

        // 2. MENYOMRÅDE: hitta synliga veckomenyer och sallader med deras text, utan Wix-genererade ID:n.
        var blocks = page.Locator("[data-testid='richTextElement']");
        var weeks = blocks.Filter(new() { HasTextRegex = new Regex(@"Vecka\s+\d", RegexOptions.IgnoreCase) });
        var salads = blocks.Filter(new() { HasTextRegex = new Regex(@"Veckans\s+sallader", RegexOptions.IgnoreCase) });
        await weeks.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await salads.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        // En trasig fontserver får inte hålla hela hämtningen öppen utan tidsgräns.
        await page.EvaluateAsync("""
            () => Promise.race([
                document.fonts.ready,
                new Promise(resolve => setTimeout(resolve, 8000))
            ]).then(() => true)
            """);

        var menu = blocks.Filter(new()
        {
            HasTextRegex = new Regex(@"Vecka\s+\d|Veckans\s+sallader|Vegetariska alternativ", RegexOptions.IgnoreCase)
        });
        var boxes = new List<LocatorBoundingBoxResult>();
        foreach (var block in await menu.AllAsync())
        {
            if (await block.IsVisibleAsync() && await block.BoundingBoxAsync() is { } box)
                boxes.Add(box);
        }
        if (boxes.Count == 0) throw new InvalidDataException("Hittade inget menyområde att fotografera.");

        // 3. BESKÄRNING: behåll originalets utseende och hela menyn, men uteslut navigation och sidfot.
        var left = Math.Max(0, boxes.Min(box => box.X) - 12);
        var top = Math.Max(0, boxes.Min(box => box.Y) - 12);
        var right = boxes.Max(box => box.X + box.Width) + 12;
        var bottom = boxes.Max(box => box.Y + box.Height) + 12;
        cancellationToken.ThrowIfCancellationRequested();
        return await page.ScreenshotAsync(new()
        {
            Type = ScreenshotType.Png,
            FullPage = true,
            Clip = new() { X = left, Y = top, Width = right - left, Height = bottom - top },
            Animations = ScreenshotAnimations.Disabled,
            Timeout = 30_000
        });
    }
}
