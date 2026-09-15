using PizzaApp.Models;

namespace PizzaApp.Services
{
    public interface IRestaurantApiService
    {
        Task<List<Restaurant>> GetRestaurantsAsync();
    }
}