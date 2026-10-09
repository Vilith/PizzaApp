using Microsoft.Extensions.DependencyInjection;

namespace PizzaApp.Services;

public static class ClientServiceRegistration
{
    // Only for the WebAssembly client. A browser tab owns its own service provider.
    public static IServiceCollection AddPizzaBrowserClient(this IServiceCollection services, Uri apiUrl)
    {
        // HttpClientFactory creates handler scopes. They must share the tab's login.
        // Never register this singleton session in the API/server's container.
        services.AddSingleton<IAuthService, AuthService>();
        services.AddTransient<AuthenticatedApiHandler>();
        services.AddHttpClient<IStatisticsApiService, StatisticsApiService>(client => client.BaseAddress = apiUrl)
            .AddHttpMessageHandler<AuthenticatedApiHandler>();
        services.AddHttpClient<ISperringApiService, SperringApiService>(client => client.BaseAddress = apiUrl)
            .AddHttpMessageHandler<AuthenticatedApiHandler>();
        services.AddHttpClient("PublicApi", client => client.BaseAddress = apiUrl);
        services.AddHttpClient("SupabaseAuth");
        services.AddHttpClient<IOrderApiService, OrderApiService>(client => client.BaseAddress = apiUrl)
            .AddHttpMessageHandler<AuthenticatedApiHandler>();
        services.AddHttpClient<IRestaurantApiService, RestaurantApiService>(client => client.BaseAddress = apiUrl)
            .AddHttpMessageHandler<AuthenticatedApiHandler>();

        return services;
    }
}
