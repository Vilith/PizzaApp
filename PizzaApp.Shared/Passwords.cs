using System.ComponentModel.DataAnnotations;

namespace PizzaApp.Shared;

public sealed class ChangePasswordInput
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8, ErrorMessage = "Använd 8–128 tecken.")]
    public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword), ErrorMessage = "Lösenorden matchar inte.")]
    public string ConfirmPassword { get; set; } = "";
}
public sealed class ResetPasswordInput
{
    public Guid UserId { get; set; }
    [Required, StringLength(128)]
    public string AdminPassword { get; set; } = "";
}
public record PasswordAccount(Guid Id, string DisplayName, string? Email);
public record TemporaryPassword(string Password);
