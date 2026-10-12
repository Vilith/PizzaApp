using System.Net.Http.Json;
using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface ISperringApiService
{
    Task<SperringPage> GetAsync();
    Task SaveAsync(SperringMenu menu);
    Task SaveRegularAsync(SperringRegularInput input) => throw new NotSupportedException();
    Task CloseWeekAsync(SperringCloseInput input) => throw new NotSupportedException();
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
    public async Task SaveRegularAsync(SperringRegularInput input)
    {
        using var response = await http.PutAsJsonAsync("api/sperring/regular", input);
        if (!response.IsSuccessStatusCode)
            throw new OrderApiException((await response.Content.ReadFromJsonAsync<Problem>())?.Detail ?? "Rätterna kunde inte sparas.");
    }
    public async Task CloseWeekAsync(SperringCloseInput input)
    {
        using var response = await http.PostAsJsonAsync("api/sperring/close-week", input);
        if (!response.IsSuccessStatusCode)
            throw new OrderApiException((await response.Content.ReadFromJsonAsync<Problem>())?.Detail ?? "Veckan kunde inte avslutas.");
    }
    private record Problem(string? Detail);
}
