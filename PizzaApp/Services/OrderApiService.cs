using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PizzaApp.Services
{
    public class OrderApiService : IOrderApiService
    {
        private readonly HttpClient _httpClient;

        public OrderApiService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<List<PizzaOrder>> GetOrdersAsync()
        {

            var orders = await _httpClient.GetFromJsonAsync<List<PizzaOrder>>("api/orders");

            return orders ?? new List<PizzaOrder>();
        }
    }
}
