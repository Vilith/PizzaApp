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

            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem()
                {
                    Id = 1,
                    Name = "Margharita",
                    Price = 95,
                    RestaurantId = 1
                },
                new MenuItem()
                {
                    Id = 2,
                    Name = "Vesuvio",
                    Price = 95,
                    RestaurantId = 1
                },
                new MenuItem()
                {
                    Id = 3,
                    Name = "Capricciosa",
                    Price = 95,
                    RestaurantId = 1
                },
                new MenuItem()
                {
                    Id = 4,
                    Name = "Hawaii",
                    Price = 95,
                    RestaurantId = 1
                });
        }
    }
}
