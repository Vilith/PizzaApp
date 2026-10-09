
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;

namespace PizzaApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();            
            builder.Services.AddOpenApi();
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.Configure<OrderingOptions>(builder.Configuration.GetSection("Ordering"));
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<StatisticsService>();
            builder.Services.Configure<StatisticsOptions>(builder.Configuration.GetSection("Statistics"));
            builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
            builder.Services.AddScoped<RegistrationService>();
            builder.Services.AddHttpClient("SupabaseRegistration", client => client.Timeout = TimeSpan.FromSeconds(20))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.AddPolicy("Registration", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                    { PermitLimit = 20, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, CurrentUser>();
            builder.Services.AddHttpClient("SupabasePasswords", client => client.Timeout = TimeSpan.FromSeconds(15))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddHttpClient("SupabaseVerification", client => client.Timeout = TimeSpan.FromSeconds(15))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddAuthentication("Supabase")
                .AddScheme<AuthenticationSchemeOptions, SupabaseAuthenticationHandler>("Supabase", _ => { });
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireRole("admin"));
            });

            builder.Services.AddDbContext<PizzaDbContext>(options => 
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseWebAssemblyDebugging();
            }
            else app.UseExceptionHandler();

            app.UseHttpsRedirection();
            // The app shell is public; API endpoints still enforce authentication.
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant();
                if (context.User.HasClaim("pizza_password_change_required", "true")
                    && context.Request.Path.StartsWithSegments("/api")
                    && path is not ("/api/auth/me" or "/api/passwords/change"))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { detail = "Byt ditt tillfälliga lösenord under Inställningar innan du fortsätter." });
                    return;
                }
                await next(context);
            });

            app.MapControllers();
            // Unknown API URLs must not return the SPA's HTML document.
            app.MapFallback("/api/{**path}", () => Results.NotFound());
            app.MapFallbackToFile("index.html").AllowAnonymous();

            app.Run();
        }
    }
}
