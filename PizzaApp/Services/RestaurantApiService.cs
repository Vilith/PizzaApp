using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Services
{
    public class RestaurantApiService : IRestaurantApiService
    {
        private readonly HttpClient _httpClient;

        public RestaurantApiService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<Restaurant>> GetRestaurantsAsync()
        {
            var restaurants = await _httpClient.GetFromJsonAsync<List<Restaurant>>("api/restaurants");

            return restaurants ?? new List<Restaurant>();
        }
    }
}
