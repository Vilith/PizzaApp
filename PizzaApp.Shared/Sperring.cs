// Veckomenyer, återkommande rätter och kontrakt för Sperrings beställningssida.

using System.Globalization;

namespace PizzaApp.Shared;

public class SperringMenu
{
    public static readonly string[] DailyChoices = ["Dagens rätt", "Dagens sallad"];
    public int Slot { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }
    public string RegularDishes { get; set; } = "";
    public string[] Days { get; set; } = ["", "", "", "", ""];
    public Guid Revision { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public DateOnly Monday => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, Week, DayOfWeek.Monday));
    public static string[] Lines(string text) => text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    public IEnumerable<string> Dishes(int day) => Lines(RegularDishes).Concat(DailyChoices).Distinct(StringComparer.OrdinalIgnoreCase);
}

public record SperringDish(int Id, string Name);
public record SperringPage(int RestaurantId, List<SperringMenu> Menus, List<SperringDish> Dishes)
{
    public string RegularDishes { get; init; } = "";
    public Guid RegularRevision { get; init; }
}
public record SperringRegularInput(string Dishes, Guid Revision);
public record SperringCloseInput(Dictionary<int, Guid> Revisions);

// Originalmenyn visas som text och får sin höjd från innehållet, utan iframe.
public record SperringWebsiteMenu(string[] Lines)
{
    public const string SourceUrl = "https://www.sperringsmat.se/lunch-meny";
}

// PNG-bild och hämtningstid. IsStale betyder att senaste uppdateringen misslyckades.
public record SperringMenuImage(string DataUrl, DateTimeOffset CapturedAt, bool IsStale = false);
