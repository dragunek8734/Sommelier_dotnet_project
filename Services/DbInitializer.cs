// Services/DbInitializer.cs
using System;
using System.Threading.Tasks;
using dotnetprojekt.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Services
{
    public class DbInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(
            IServiceProvider serviceProvider,
            ILogger<DbInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing database and creating materialized views");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WineLoversContext>();

                // Run migrations
                await dbContext.Database.MigrateAsync(cancellationToken);

                // Ensure the pg_cron extension is installed
                await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_cron;", cancellationToken);

                // Create materialized view for top rated wines
                // First check if the view exists
                var viewExists = false;
                try
                {
                    // Execute a COUNT query which is more reliable for checking existence
                    var query = "SELECT EXISTS (SELECT 1 FROM pg_matviews WHERE matviewname = 'mv_top_rated_wines')";
                    var result = await dbContext.Database
                        .SqlQueryRaw<bool>(query)
                        .FirstOrDefaultAsync(cancellationToken);
                    viewExists = result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check if materialized view exists");
                    viewExists = false;
                }

                if (!viewExists)
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        CREATE MATERIALIZED VIEW mv_top_rated_wines AS
                        SELECT
                            w.""Id"" AS wine_id,
                            w.""Name"" AS name,
                            AVG(r.""RatingValue"")::numeric(10,2) AS average_rating
                        FROM public.""Wines"" w
                        JOIN public.""Ratings"" r ON w.""Id"" = r.""WineId""
                        WHERE w.""Id"" IS NOT NULL
                        GROUP BY w.""Id"", w.""Name""
                        ORDER BY AVG(r.""RatingValue"") DESC
                        LIMIT 20", cancellationToken);
                }

                // Schedule daily refresh using pg_cron - wrapped in try/catch as it may not be available
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        SELECT cron.schedule(
                            'refresh_top_rated_wines_daily',
                            '0 0 * * *',
                            $$REFRESH MATERIALIZED VIEW mv_top_rated_wines$$
                        );", cancellationToken);
                }
                catch (Exception cronEx)
                {
                    _logger.LogWarning(cronEx, "Could not schedule automatic refresh with pg_cron. The materialized view will need to be refreshed manually.");
                }

                // Perform an initial refresh of the materialized view
                await dbContext.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW mv_top_rated_wines;", cancellationToken);

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                // Don't throw - allow the application to continue even if materialized view setup fails
                // The application can still function with regular queries
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
