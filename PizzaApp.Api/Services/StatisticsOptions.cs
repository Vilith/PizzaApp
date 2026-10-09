namespace PizzaApp.Api.Services;

public class StatisticsOptions
{
    public DateOnly? StartDate { get; set; }
    public List<string> ExcludedNames { get; set; } = ["tff"];
    public List<DateOnly> ExcludedDates { get; set; } = [];
}
