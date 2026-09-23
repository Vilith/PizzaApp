namespace PizzaApp.Api.Models;

// Immutable snapshot; menu changes and later days cannot change this history.
public class CompletedOrderDay
{
    public int RestaurantId { get; set; }
    public DateOnly Date { get; set; }
    public string RestaurantName { get; set; } = "";
    public bool IsPizzeria { get; set; }
    public DateTime CollectedAt { get; set; }
    public string CollectorsJson { get; set; } = "[]";
    public string OrdersJson { get; set; } = "[]";
}
