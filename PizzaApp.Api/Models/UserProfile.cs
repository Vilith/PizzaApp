namespace PizzaApp.Api.Models;

public class UserProfile
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string? AvatarDataUrl { get; set; }
    public Guid Revision { get; set; }
}
