namespace PizzaApp.Api.Models
{
    public class PizzaOrder
    {
        public Guid? OwnerUserId { get; set; }
        public decimal? UnitPrice { get; set; }
        public bool CanCollect { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Pizza { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        // Null on legacy rows: their restaurant cannot be reliably inferred.
        public int? RestaurantId { get; set; }
        public int? MenuItemId { get; set; }
        public DateOnly? OrderDate { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Sauce { get; set; }
        public string? Drink { get; set; }
        public Guid Revision { get; set; }
    }
}
