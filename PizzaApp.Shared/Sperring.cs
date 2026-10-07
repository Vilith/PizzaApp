using System.Globalization;

namespace PizzaApp.Shared;

public class SperringMenu
{
    public int Slot { get; set; }
    public int Year { get; set; }
    public int Week { get; set; }
    public string RegularDishes { get; set; } = "";
    public string[] Days { get; set; } = ["", "", "", "", ""];
    public Guid Revision { get; set; }
    public DateOnly Monday => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, Week, DayOfWeek.Monday));
    public static string[] Lines(string text) => text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    public IEnumerable<string> Dishes(int day) => Lines(RegularDishes).Concat(Lines(Days[day])).Distinct(StringComparer.OrdinalIgnoreCase);
}

public record SperringDish(int Id, string Name);
public record SperringPage(int RestaurantId, List<SperringMenu> Menus, List<SperringDish> Dishes);
