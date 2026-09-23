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
        public DbSet<CompletedOrderDay> CompletedOrderDays => Set<CompletedOrderDay>();

        public DbSet<Restaurant> Restaurants => Set<Restaurant>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CompletedOrderDay>().HasKey(d => new { d.RestaurantId, d.Date });
            modelBuilder.Entity<CompletedOrderDay>().HasOne<Restaurant>().WithMany()
                .HasForeignKey(d => d.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PizzaOrder>().HasIndex(o => new { o.RestaurantId, o.OrderDate });
            modelBuilder.Entity<PizzaOrder>().Property(o => o.Revision).IsConcurrencyToken();
            modelBuilder.Entity<PizzaOrder>().Property(o => o.Quantity).HasDefaultValue(1);
            modelBuilder.Entity<PizzaOrder>().HasOne<Restaurant>().WithMany()
                .HasForeignKey(o => o.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PizzaOrder>().HasOne<MenuItem>().WithMany()
                .HasForeignKey(o => o.MenuItemId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Restaurant>().HasData(
                new Restaurant()
                {
                    Id = 1,
                    Name = "Kvänum Pizzeria",
                    IsPizzeria = true

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
                },
                // Example dishes from the wireframe; replace with the restaurant's actual menu.
                new MenuItem { Id = 5, Name = "Oxfilé med potatis", Price = 189, RestaurantId = 2 },
                new MenuItem { Id = 6, Name = "Kycklingfilé", Price = 159, RestaurantId = 2 },
                new MenuItem { Id = 7, Name = "Pasta Carbonara", Price = 149, RestaurantId = 2 },
                new MenuItem { Id = 8, Name = "Caesarsallad", Price = 139, RestaurantId = 2 });
        }
    }
}
