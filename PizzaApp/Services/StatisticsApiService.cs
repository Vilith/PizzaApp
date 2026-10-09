using System.Net.Http.Json;
using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface IStatisticsApiService
{
    Task<PizzeriaStatistics> GetAsync(int? year = null);
}

public class StatisticsApiService(HttpClient client) : IStatisticsApiService
{
    public async Task<PizzeriaStatistics> GetAsync(int? year = null) =>
        await client.GetFromJsonAsync<PizzeriaStatistics>("api/statistics/pizzeria" +
            (year.HasValue ? $"?year={year.Value}" : ""))
        ?? throw new HttpRequestException("Statistiken saknas i svaret.");
}
