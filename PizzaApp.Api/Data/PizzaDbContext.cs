using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Models;

namespace PizzaApp.Api.Data
{
    public class PizzaDbContext : DbContext
    {
        public PizzaDbContext(DbContextOptions<PizzaDbContext> options) 
            : base(options) 
        { }

        public DbSet<PizzaOrder> Orders => Set<PizzaOrder>();
    }
}
