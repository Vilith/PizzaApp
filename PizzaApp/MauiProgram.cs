using Microsoft.Extensions.Logging;
using PizzaApp.Services;

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

            builder.Services.AddHttpClient<IOrderApiService, OrderApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7113/");
            });

            builder.Services.AddHttpClient<IRestaurantApiService, RestaurantApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7113/");
            });


            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
