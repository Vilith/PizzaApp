namespace PizzaApp.Api.Models;

public class SperringWeek
{
    public int Id { get; set; }
    public string MenuJson { get; set; } = "";
    public Guid Revision { get; set; }
}
