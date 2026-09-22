using System.ComponentModel.DataAnnotations;

namespace PizzaApp.Shared;

public static class PizzaChoices
{
    public static readonly string[] Sauces = ["Vitlökssås", "Bearnaisesås", "Chilisås", "Ingen sås"];
    public static readonly string[] Drinks = ["Coca-Cola 33 cl", "Coca-Cola Zero 33 cl", "Fanta 33 cl", "Sprite 33 cl", "Vatten"];
}

public class OrderInput
{
    [Range(1, int.MaxValue)] public int MenuItemId { get; set; }
    [Range(1, 99, ErrorMessage = "Ange mellan 1 och 99 portioner.")]
    public int Quantity { get; set; } = 1;
    [StringLength(100, ErrorMessage = "Namnet får innehålla högst 100 tecken.")]
    public string Name { get; set; } = "";
    [StringLength(500, ErrorMessage = "Kommentaren får innehålla högst 500 tecken.")]
    public string? Comment { get; set; }
    public string? Sauce { get; set; }
    public string? Drink { get; set; }
    // Used on updates to detect another person's changes.
    public Guid Revision { get; set; }
}

public class OrderDetails : OrderInput
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Pizza { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record OrderSummary(string Pizza, string? Sauce, string? Drink, string? Comment, int Quantity);

public class DailyOrderList
{
    public DateOnly Date { get; set; }
    public bool IsPizzeria { get; set; }
    public string Deadline { get; set; } = "11:15";
    public bool DeadlinePassed { get; set; }
    public bool IsLocked { get; set; }
    public List<OrderDetails> Orders { get; set; } = [];
    public List<OrderSummary> Summary { get; set; } = [];
}
