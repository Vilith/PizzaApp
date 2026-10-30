using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using PizzaApp.Shared;

namespace PizzaApp.Api.Services;

// CACHE: alla besökare delar senaste PNG-bilden. Endast en webbläsare får ta en ny bild i taget.
public sealed class SperringScreenshotService(
    ISperringScreenshotCapture capture,
    TimeProvider clock,
    IOptions<SperringScreenshotOptions> options,
    ILogger<SperringScreenshotService> logger) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private SperringMenuImage? latest;
    private DateTimeOffset nextAttempt;

    // 1. UPPDATERING: första besöket efter intervallet tar en ny bild, övriga får den sparade.
    public async Task<SperringMenuImage> GetAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var now = clock.GetUtcNow();
            if (now < nextAttempt)
                return latest ?? throw new InvalidDataException("Menybilden är tillfälligt otillgänglig.");

            try
            {
                var bytes = await capture.CaptureAsync(cancellationToken);
                if (bytes.Length == 0) throw new InvalidDataException("Skärmbilden blev tom.");
                latest = new SperringMenuImage("data:image/png;base64," + Convert.ToBase64String(bytes), clock.GetUtcNow());
                nextAttempt = clock.GetUtcNow().AddMinutes(Math.Max(5, options.Value.RefreshMinutes));
            }
            // 2. KÄLLFEL: behåll tidigare bild och vänta 15 minuter innan ett nytt automatiskt försök.
            catch (Exception ex) when (ex is PlaywrightException or HttpRequestException or InvalidDataException)
            {
                logger.LogWarning(ex, "Kunde inte uppdatera Sperrings menybild.");
                nextAttempt = clock.GetUtcNow().AddMinutes(15);
                if (latest == null) throw;
                latest = latest with { IsStale = true };
            }

            return latest!;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();
}
