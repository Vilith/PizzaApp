using PizzaApp.Models;

namespace PizzaApp.Services
{
    public interface IOrderApiService
    {
        Task<List<PizzaOrder>> GetOrdersAsync();
        Task<PizzaOrder?> CreateOrderAsync(PizzaOrder order);
    }
}