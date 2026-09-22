using System.Net.Http.Json;
using PizzaApp.Shared;

namespace PizzaApp.Services;

public sealed class OrderApiException(string message) : Exception(message);

public class OrderApiService(HttpClient httpClient) : IOrderApiService
{
    private static string Url(int restaurantId) => $"api/restaurants/{restaurantId}/orders";

    public async Task<DailyOrderList> GetTodayAsync(int restaurantId)
    {
        using var response = await httpClient.GetAsync(Url(restaurantId));
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<DailyOrderList>()
            ?? throw new HttpRequestException("Servern returnerade ingen beställningslista.");
    }

    public async Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null)
    {
        using var response = id.HasValue
            ? await httpClient.PutAsJsonAsync($"{Url(restaurantId)}/{id}", input)
            : await httpClient.PostAsJsonAsync(Url(restaurantId), input);
        await EnsureSuccess(response);
        return await response.Content.ReadFromJsonAsync<OrderDetails>()
            ?? throw new HttpRequestException("Servern returnerade ingen beställning.");
    }

    public async Task DeleteAsync(int restaurantId, int id, Guid revision)
    {
        using var response = await httpClient.DeleteAsync($"{Url(restaurantId)}/{id}?revision={revision}");
        await EnsureSuccess(response);
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if ((int)response.StatusCode is 400 or 404 or 409)
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
            throw new OrderApiException(problem?.Detail ?? "Kontrollera uppgifterna och uppdatera listan.");
        }
        response.EnsureSuccessStatusCode();
    }

    private sealed class ApiProblem { public string? Detail { get; set; } }
}
