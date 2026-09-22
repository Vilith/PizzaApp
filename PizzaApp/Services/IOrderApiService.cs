using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface IOrderApiService
{
    Task<DailyOrderList> GetTodayAsync(int restaurantId);
    Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null);
    Task DeleteAsync(int restaurantId, int id, Guid revision);
}
