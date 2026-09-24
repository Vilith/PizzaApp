namespace PizzaApp.Shared;

public record AuthConfiguration(string Url, string PublishableKey);
public record SignedInUser(Guid Id, string Email, bool IsAdmin, string DisplayName = "", string? AvatarDataUrl = null, Guid ProfileRevision = default);
