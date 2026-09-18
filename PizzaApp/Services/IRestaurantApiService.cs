using PizzaApp.Models;
using MenuItemModel = PizzaApp.Models.MenuItem;

namespace PizzaApp.Services
{
    public interface IRestaurantApiService
    {
        Task<List<Restaurant>> GetRestaurantsAsync();
        Task<List<MenuItemModel>> GetMenuAsync(int restaurantId);
    }
}