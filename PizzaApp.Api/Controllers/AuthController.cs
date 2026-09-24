using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PizzaApp.Api.Services;
using PizzaApp.Shared;
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;

namespace PizzaApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IOptions<SupabaseOptions> options, ICurrentUser user, PizzaDbContext db) : ControllerBase
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
    public async Task<SignedInUser> Me()
    {
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == user.Id);
        return Details(profile);
    }

    [HttpPut("profile")]
    [RequestSizeLimit(400000)]
    public async Task<ActionResult<SignedInUser>> UpdateProfile(ProfileInput input)
    {
        var name = input.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl))
            return Problem(statusCode: 400, detail: "Ange ett namn eller nick utan radbrytningar.");
        if (!ProfileImages.IsValid(input.AvatarDataUrl))
            return Problem(statusCode: 400, detail: "Välj en giltig profilbild, högst 256 × 256 pixlar och 256 kB i PNG-format.");
        var profile = await db.UserProfiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
        if ((profile?.Revision ?? Guid.Empty) != input.Revision)
            return Problem(statusCode: 409, detail: "Profilen har ändrats. Hämta profilen igen innan du sparar.");
        if (profile == null)
        {
            profile = new UserProfile { UserId = user.Id };
            db.UserProfiles.Add(profile);
        }
        profile.DisplayName = name;
        profile.AvatarDataUrl = input.AvatarDataUrl;
        profile.Revision = Guid.NewGuid();
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        { return Problem(statusCode: 409, detail: "Profilen kunde inte sparas. Hämta profilen igen och försök på nytt."); }
        return Details(profile);
    }

    private SignedInUser Details(UserProfile? profile) => new(user.Id, User.FindFirstValue(ClaimTypes.Email) ?? "",
        user.IsAdmin, profile?.DisplayName ?? "", profile?.AvatarDataUrl, profile?.Revision ?? Guid.Empty);
}
