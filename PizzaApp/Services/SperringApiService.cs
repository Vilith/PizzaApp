using System.Net.Http.Json;
using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface ISperringApiService
{
    Task<SperringPage> GetAsync();
    Task SaveAsync(SperringMenu menu);
}

public class SperringApiService(HttpClient http) : ISperringApiService
{
    public async Task<SperringPage> GetAsync() => await http.GetFromJsonAsync<SperringPage>("api/sperring")
        ?? throw new HttpRequestException("Menyerna kunde inte hämtas.");
    public async Task SaveAsync(SperringMenu menu)
    {
        using var response = await http.PutAsJsonAsync($"api/sperring/{menu.Slot}", menu);
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>();
            throw new OrderApiException(problem?.Detail ?? "Menyn kunde inte sparas.");
        }
    }
    private record Problem(string? Detail);
}
