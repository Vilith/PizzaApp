using Microsoft.AspNetCore.Mvc;
using PizzaApp.Api.Models;

namespace PizzaApp.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<PizzaOrder>> GetOrders()
        {
            var orders = new List<PizzaOrder>();

            return Ok(orders);
        }
    }
}
