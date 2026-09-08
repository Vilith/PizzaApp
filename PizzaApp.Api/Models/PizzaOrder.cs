namespace PizzaApp.Api.Models
{
    public class PizzaOrder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Pizza { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
