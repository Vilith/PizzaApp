using PizzaApp.Models;
using System.Net.Http.Json;
using MenuItemModel = PizzaApp.Models.MenuItem;


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


        public async Task<List<MenuItemModel>> GetMenuAsync(int restaurantId)
        {
            var menuItems = await _httpClient.GetFromJsonAsync<List<MenuItemModel>>(
                $"api/restaurants/{restaurantId}/menu");

            return menuItems ?? new List<MenuItemModel>();
        }

    }
}
