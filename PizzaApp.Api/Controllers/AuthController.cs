using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Services;
using PizzaApp.Shared;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IOptions<SupabaseOptions> options, ICurrentUser user) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("config")]
    public ActionResult<AuthConfiguration> Configuration()
    {
        if (!options.Value.IsConfigured)
            return Problem(statusCode: 503, detail: "Administratören behöver konfigurera Supabase Auth.");
        return new AuthConfiguration(options.Value.Url.TrimEnd('/'), options.Value.PublishableKey);
    }

    [HttpGet("me")]
    public SignedInUser Me() => new(user.Id, User.FindFirstValue(ClaimTypes.Email) ?? "", user.IsAdmin);
}
