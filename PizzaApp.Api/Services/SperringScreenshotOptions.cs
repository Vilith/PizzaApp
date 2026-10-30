namespace PizzaApp.Api.Services;

// MENYBILD: uppdateringsintervall och valfri lokal webbläsarkanal, exempelvis msedge.
public class SperringScreenshotOptions
{
    public int RefreshMinutes { get; set; } = 360;
    public string? BrowserChannel { get; set; }
}
