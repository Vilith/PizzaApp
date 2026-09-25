using System.ComponentModel.DataAnnotations;

namespace PizzaApp.Shared;

public class RegistrationEmailInput
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
}

public class RegistrationInput : RegistrationEmailInput
{
    [Required(ErrorMessage = "Ange ett lösenord."), StringLength(128, MinimumLength = 8, ErrorMessage = "Lösenordet ska innehålla 8–128 tecken.")]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "Ange ditt nick."), StringLength(100)]
    public string DisplayName { get; set; } = "";
}
