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
        
        
    }
}
