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

        public DbSet<Restaurant> Restaurants => Set<Restaurant>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Restaurant>().HasData(
                new Restaurant()
                {
                    Id = 1,
                    Name = "Kvänum Pizzeria"

                },
                new Restaurant()
                {
                    Id = 2,
                    Name = "Sperring"
                });
        }
    }
}
