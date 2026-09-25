namespace PizzaApp.Api.Models;

public class UserProfile
{
    public Guid UserId { get; set; }
    public bool IsRegisteredMember { get; set; }
    public string DisplayName { get; set; } = "";
    public string AliasKey { get; set; } = "";
    public string? LoginEmail { get; set; }
    public string? AvatarDataUrl { get; set; }
    public Guid Revision { get; set; }
}
