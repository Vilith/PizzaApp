namespace PizzaApp.Shared;

public record RankedCount(string Name, int Count);

public class PizzeriaStatistics
{
    public const decimal LunchPrice = 95m;
    public const decimal ExtraDrinkPrice = 15m;
    public int? Year { get; set; }
    public List<int> AvailableYears { get; set; } = [];
    public DateOnly? Since { get; set; }
    public DateOnly? StartDate { get; set; }
    // Kept for existing API clients; now counts all pizzeria meals.
    public int TotalPizzas { get; set; }
    public int TotalMeals { get; set; }
    public int ExtraDrinks { get; set; }
    public decimal ExtraDrinkValue => ExtraDrinks * ExtraDrinkPrice;
    public decimal TotalOrderValue => TotalMeals * LunchPrice + ExtraDrinkValue;
    public int TotalDrinks { get; set; }
    public int CompletedDays { get; set; }
    public List<RankedCount> TopPizzas { get; set; } = [];
    public List<RankedCount> Drinks { get; set; } = [];
    public List<RankedCount> Collectors { get; set; } = [];
}
