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
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<CompletedOrderDay> CompletedOrderDays => Set<CompletedOrderDay>();

        public DbSet<Restaurant> Restaurants => Set<Restaurant>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        public static string NormalizeAlias(string alias) => alias.Trim().ToUpperInvariant();

        private void PrepareAliases()
        {
            foreach (var entry in ChangeTracker.Entries<UserProfile>().Where(e => e.State is EntityState.Added or EntityState.Modified))
                entry.Entity.AliasKey = NormalizeAlias(entry.Entity.DisplayName);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        { PrepareAliases(); return base.SaveChanges(acceptAllChangesOnSuccess); }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        { PrepareAliases(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserProfile>().HasKey(p => p.UserId);
            modelBuilder.Entity<UserProfile>().Property(p => p.DisplayName).HasMaxLength(100);
            modelBuilder.Entity<UserProfile>().Property(p => p.AliasKey).HasMaxLength(100);
            modelBuilder.Entity<UserProfile>().HasIndex(p => p.AliasKey).IsUnique();
            modelBuilder.Entity<UserProfile>().Property(p => p.LoginEmail).HasMaxLength(254);
            modelBuilder.Entity<UserProfile>().Property(p => p.AvatarDataUrl).HasMaxLength(350000);
            modelBuilder.Entity<UserProfile>().Property(p => p.Revision).IsConcurrencyToken();
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
                new MenuItem { Id = 1, MenuNumber = 1, Name = "Margherita", Description = "Tomat, Ost", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 2, MenuNumber = 2, Name = "Vesuvio", Description = "Skinka", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 3, MenuNumber = 3, Name = "Capricciosa", Description = "Skinka, Champinjoner", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 4, MenuNumber = 4, Name = "Hawaii", Description = "Skinka, Ananas", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 9, MenuNumber = 5, Name = "Calzone", Description = "Skinka (Inbakad)", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 10, MenuNumber = 6, Name = "Pescatore", Description = "Tonfisk, Lök", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 11, MenuNumber = 7, Name = "Caruso", Description = "Köttfärs, Vitlökssås", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 12, MenuNumber = 8, Name = "Bolognese", Description = "Köttfärssås, Lök", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 13, MenuNumber = 9, Name = "La Maffia", Description = "Bacon, Lök", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 14, MenuNumber = 10, Name = "Cacciatore", Description = "Salami", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 15, MenuNumber = 11, Name = "Tomaso", Description = "Skinka, Räkor", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 16, MenuNumber = 12, Name = "Marinara", Description = "Musslor, Räkor", Price = 85, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 17, MenuNumber = 13, Name = "Africana", Description = "Skinka, Banan, Ananas, Curry", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 18, MenuNumber = 14, Name = "Jamaica", Description = "Skinka, Champinjoner, Räkor", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 19, MenuNumber = 15, Name = "Mama Mia", Description = "Bacon, Champinjoner, Lök, Paprika", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 20, MenuNumber = 16, Name = "Amore Mio", Description = "Fläskfilé, Lök, Champinjoner, Bearnaisesås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 21, MenuNumber = 17, Name = "Ciao Ciao", Description = "Fläskfilé, Lök, Champinjoner, Vitlökssås (Inbakad)", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 22, MenuNumber = 18, Name = "Prinsessa", Description = "Skinka, Ananas, Räkor", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 23, MenuNumber = 19, Name = "Papillon", Description = "Skinka, Bacon, Lök, Bearnaisesås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 24, MenuNumber = 20, Name = "Kycklingpizza", Description = "Kyckling, Ananas, Jordnötter, Curry", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 25, MenuNumber = 21, Name = "Vegetariana", Description = "Champinjoner, Ananas, Paprika, Lök, Tomat, Sparris", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 26, MenuNumber = 22, Name = "Kebabpizza", Description = "Kebabkött, Lök, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 27, MenuNumber = 23, Name = "Gorgonzola", Description = "Champinjoner, Oxfilé, Gorgonzolaost", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 28, MenuNumber = 24, Name = "Disco", Description = "Skinka, Köttfärssås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 29, MenuNumber = 25, Name = "Rolandpizza", Description = "Skinka, Kebabkött, Champinjoner, Bearnaisesås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 30, MenuNumber = 26, Name = "Alexpizza", Description = "Kebabkött, Lök, Paprika, Champinjoner, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 31, MenuNumber = 27, Name = "Cyckelpizza", Description = "Kebabkött, Lök, Paprika, Stark kebabsås, Vitlökssås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 32, MenuNumber = 28, Name = "Husets pizza", Description = "Oxfilé, Champinjoner, Sparris, Bearnaisesås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 33, MenuNumber = 29, Name = "Quatro Stagioni", Description = "Skinka, Musslor, Räkor, Champinjoner", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 34, MenuNumber = 30, Name = "Mexicana", Description = "Köttfärssås, Champinjoner, Lök, Tacosås, Vitlökssås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 35, MenuNumber = 31, Name = "Azteka", Description = "Skinka, Tacosås, Jalapeno, Lök, Vitlökssås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 36, MenuNumber = 32, Name = "Acapulko", Description = "Oxfilé, Champinjoner, Lök, Jalapeno, Tacosås, Vitlökssås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 37, MenuNumber = 33, Name = "Kebabspecial", Description = "Kebabkött, Gurka, Tomat, Isbergssallad, Kebabsås, Lök", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 38, MenuNumber = 34, Name = "Vara Special", Description = "Kebabkött, Skinka, Pommes, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 39, MenuNumber = 35, Name = "Tre Kronor", Description = "Kebabkött, Skinka, Ananas, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 40, MenuNumber = 36, Name = "Flygande Tefat", Description = "Fläskfilé, Champinjoner, Paprika, Lök, Bearnaisesås (Dubbel inbakad)", Price = 100, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 41, MenuNumber = 37, Name = "U-båt 1", Description = "Fläskfilé, Champinjoner, Paprika, Lök, Bearnaisesås (Halvt inbakad)", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 42, MenuNumber = 38, Name = "U-båt 2", Description = "Skinka, Kebabkött, Champinjoner, Kebabsås (Halvt inbakad)", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 43, MenuNumber = 39, Name = "Kycklinggryta", Description = "Kyckling, Räkor, Champinjoner, Lök, Paprika, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 44, MenuNumber = 40, Name = "Frank Special", Description = "Kebabkött, Skinka, Räkor, Ananas, Champinjoner, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 45, MenuNumber = 41, Name = "Kvänums Special", Description = "Skinka, Kebabkött, Bacon, Champinjoner, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 46, MenuNumber = 42, Name = "Anderspizza", Description = "Skinka, Lök, Paprika, Champinjoner, Kebabkött, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 47, MenuNumber = 43, Name = "Curuso Special", Description = "Skinka, Champinjoner, Köttfärs, Pommes, Kebabsås", Price = 90, Category = "Pizzor", RestaurantId = 1 },
                new MenuItem { Id = 48, Category = "Sallader", Name = "Amerikansk Sallad", Description = "Skinka, Räkor, Ananas, Ost, Sallad, Gurka, Majs", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 49, Category = "Sallader", Name = "Räksallad", Description = "Räkor, Ost, Ägg, Citron, Tomat, Sallad, Gurka, Majs", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 50, Category = "Sallader", Name = "Västkustsallad", Description = "Räkor, Musslor, Champinjoner, Majs, Citron, Tomat, Sallad, Gurka", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 51, Category = "Sallader", Name = "Kycklingsallad", Description = "Kyckling, Ananas, Majs, Tomat, Sallad, Gurka", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 52, Category = "Sallader", Name = "Tonfisksallad", Description = "Tonfisk, Ost, Lök, Majs, Champinjoner, Tomat, Sallad, Gurka", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 53, Category = "Sallader", Name = "Kebabsallad", Description = "Kebab, Lök, Majs, Champinjoner, Tomat, Sallad, Gurka", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 54, Category = "Sallader", Name = "Tacosallad", Description = "Nötfärs, Majs, Lök, Ost, Ananas, Tomat, Gurka, Sallad, Tacosås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 55, Category = "Sallader", Name = "Vegetarisk Sallad", Description = "Sallad, Gurka, Ananas, Champinjoner, Paprika, Majs, Sparris", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 56, Category = "Kebab", Name = "Kebab m. Bröd", Description = "Kebab eller kyckling, Sallad, Tomat, Gurka, Lök, Kebabsås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 57, Category = "Kebab", Name = "Kebab/Kyckling-Tallrik", Description = "Pommes, Sallad, Gurka, Tomat, Lök, Kebabsås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 58, Category = "Kebab", Name = "Kebab/Kyckling-Rulle", Description = "Kebab eller kyckling, Sallad, Tomat, Gurka, Lök, Kebabsås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 59, Category = "Kebab", Name = "Räkrulle", Description = "Räkor, Sallad, Gurka, Tomat, Skinka, Ananas, Kebabsås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 60, Category = "Kebab", Name = "Hawaiirulle", Description = "Skinka, Sallad, Gurka, Tomat, Ananas, Kebabsås", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 61, Category = "Kebab", Name = "Tacorulle", Description = "Nötfärs, Majs, Lök, Gurka, Tomat, Sallad, Tacosås, Jalapeno", Price = 90, RestaurantId = 1 },
                new MenuItem { Id = 65, Category = "Stekrätter", Name = "Hamburgare 90gr", Description = "Med bröd & Pommes", Price = 80, RestaurantId = 1 },

                // Example dishes from the wireframe; replace with the restaurant's actual menu.
                new MenuItem { Id = 5, Name = "Oxfilé med potatis", Price = 189, RestaurantId = 2 },
                new MenuItem { Id = 6, Name = "Kycklingfilé", Price = 159, RestaurantId = 2 },
                new MenuItem { Id = 7, Name = "Pasta Carbonara", Price = 149, RestaurantId = 2 },
                new MenuItem { Id = 8, Name = "Caesarsallad", Price = 139, RestaurantId = 2 });
        }
    }
}
