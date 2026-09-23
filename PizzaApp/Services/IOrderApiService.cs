using PizzaApp.Shared;

namespace PizzaApp.Services;

public interface IOrderApiService
{
    Task<DailyOrderList> GetTodayAsync(int restaurantId);
    Task<DailyOrderList> SetCollectorAsync(int restaurantId, int id, CollectorInput input);
    Task<DailyOrderList> CompleteAsync(int restaurantId, CompleteDayInput input);
    Task<OrderDetails> SaveAsync(int restaurantId, OrderInput input, int? id = null);
    Task DeleteAsync(int restaurantId, int id, Guid revision);
}
