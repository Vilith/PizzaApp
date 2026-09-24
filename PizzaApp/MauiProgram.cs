using Microsoft.Extensions.Logging;
using PizzaApp.Services;
using Microsoft.Extensions.Configuration;

namespace PizzaApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            using var settings = typeof(MauiProgram).Assembly.GetManifestResourceStream("PizzaApp.clientsettings.json")!;
            builder.Configuration.AddJsonStream(settings);
            var apiUrl = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7113/");
            if (apiUrl.Scheme != "https") throw new InvalidOperationException("API-adressen måste använda HTTPS.");
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddTransient<AuthenticatedApiHandler>();
            builder.Services.AddHttpClient("PublicApi", client => client.BaseAddress = apiUrl)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddHttpClient("SupabaseAuth")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
            builder.Services.AddHttpClient<IOrderApiService, OrderApiService>(client => client.BaseAddress = apiUrl)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
                .AddHttpMessageHandler<AuthenticatedApiHandler>();
            builder.Services.AddHttpClient<IRestaurantApiService, RestaurantApiService>(client => client.BaseAddress = apiUrl)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
                .AddHttpMessageHandler<AuthenticatedApiHandler>();


            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
