using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Data
{
    public static class DataSeederExtensions
    {
        public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var seeder = services.GetRequiredService<DataSeeder>();
                    await seeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<DataSeeder>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
        }

        public static IServiceCollection AddDataSeeder(this IServiceCollection services)
        {
            services.AddScoped<DataSeeder>();
            return services;
        }
    }
}
