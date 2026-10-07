using System.ComponentModel.DataAnnotations;

namespace PizzaApp.Shared;

public static class PizzaChoices
{
    public static readonly string[] Sauces = ["Ingen sås", "Vitlökssås", "Bearnaisesås", "Kebabsås", "Kebabsås (Mixad)", "Kebabsås (Stark)"];
    public static readonly string[] Drinks = ["Coca-Cola 33cl", "Fanta 33cl", "Sprite 33cl", "Pepsi Max 33cl"];
}

public static class MenuSauce
{
    public static string DefaultFor(string description)
    {
        foreach (var ingredient in description.Split(','))
        {
            var text = ingredient.Trim();
            if (text.StartsWith("Stark kebabsås", StringComparison.OrdinalIgnoreCase))
                return "Kebabsås (Stark)";
            foreach (var sauce in PizzaChoices.Sauces.Where(s => s != "Ingen sås").OrderByDescending(s => s.Length))
                if (text.Equals(sauce, StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith(sauce + " (", StringComparison.OrdinalIgnoreCase))
                    return sauce;
        }
        return "Ingen sås";
    }
}

public class OrderInput
{
    [Range(1, int.MaxValue)] public int MenuItemId { get; set; }
    [Range(1, 99, ErrorMessage = "Ange mellan 1 och 99 portioner.")]
    public int Quantity { get; set; } = 1;
    // Retained for older clients and response DTOs; the server uses the profile
    // for new orders and preserves the stored name when editing an order.
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
    public string? AvatarDataUrl { get; set; }
    public Guid? OwnerUserId { get; set; }
    public decimal? UnitPrice { get; set; }
    public bool CanCollect { get; set; }
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Pizza { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record OrderSummary(string Pizza, string? Sauce, string? Drink, string? Comment, int Quantity);

public class DailyOrderList
{
    public DateTime? CollectedAt { get; set; }
    public List<string> CollectedBy { get; set; } = [];
    public DateOnly Date { get; set; }
    public bool IsPizzeria { get; set; }
    public string Deadline { get; set; } = "11:15";
    public bool DeadlinePassed { get; set; }
    public bool IsLocked { get; set; }
    public List<OrderDetails> Orders { get; set; } = [];
    public List<OrderSummary> Summary { get; set; } = [];
}

public record CollectorInput(bool CanCollect, Guid Revision, DateOnly Date);
public record CompleteDayInput(DateOnly Date, Dictionary<int, Guid> Revisions);
