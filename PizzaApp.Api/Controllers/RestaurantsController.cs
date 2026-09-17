using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Models;

namespace PizzaApp.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly PizzaDbContext _context;

        public RestaurantsController(PizzaDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetRestaurantsAsync()
        {
            var restaurants = await _context.Restaurants.ToListAsync();

            return Ok(restaurants);
        }

        [HttpGet("{id}/menu")]
        public async Task<ActionResult<IEnumerable<MenuItem>>> GetMenuAsync(int id)
        {
            var restaurantExists = await _context.Restaurants.AnyAsync(r => r.Id == id);

            if (!restaurantExists)
            {
                return NotFound($"Restaurang med ID {id} hittades inte.");
            }

            var menuItems = await _context.MenuItems
                .Where(m => m.RestaurantId == id)
                .ToListAsync();

            return Ok(menuItems);
        }
        
        
    }
}
