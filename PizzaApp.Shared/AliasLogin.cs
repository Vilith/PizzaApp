using System.ComponentModel.DataAnnotations;

namespace PizzaApp.Shared;

public sealed class AliasLogin
{
    [Required, StringLength(100)] public string Alias { get; set; } = "";
    [Required, StringLength(128)] public string Password { get; set; } = "";
}
