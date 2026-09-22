
using Microsoft.EntityFrameworkCore;
using PizzaApp.Api.Data;
using PizzaApp.Api.Services;

namespace PizzaApp.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();            
            builder.Services.AddOpenApi();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.Configure<OrderingOptions>(builder.Configuration.GetSection("Ordering"));
            builder.Services.AddScoped<OrderService>();

            builder.Services.AddDbContext<PizzaDbContext>(options => 
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
