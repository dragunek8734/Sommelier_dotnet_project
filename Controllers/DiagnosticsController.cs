using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Context;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(WineLoversContext context, ILogger<DiagnosticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> DatabaseStats()
        {
            var stats = new Dictionary<string, object>();
            var stopwatch = new Stopwatch();
            
            // Count total records in main tables
            stopwatch.Start();
            stats["TotalWines"] = await _context.Wines.CountAsync();
            stats["TotalWineTypes"] = await _context.WineTypes.CountAsync();
            stats["TotalCountries"] = await _context.Countries.CountAsync();
            stats["TotalRegions"] = await _context.Regions.CountAsync();
            stats["TotalGrapes"] = await _context.Grapes.CountAsync();
            stats["TotalWineries"] = await _context.Wineries.CountAsync();
            stats["TotalRatings"] = await _context.Ratings.CountAsync();
            stats["CountingTime"] = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            
            // Test query performance for a simple query
            stopwatch.Start();
            await _context.Wines.Take(50).ToListAsync();
            stats["SimpleQueryTime"] = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            
            // Test query performance for a complex query with joins
            stopwatch.Start();
            await _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Acidity)
                .Include(w => w.Ratings)
                .Take(50)
                .ToListAsync();
            stats["ComplexQueryTime"] = stopwatch.ElapsedMilliseconds;
            stopwatch.Reset();
            
            // Test performance of array property filtering
            stopwatch.Start();
            var winesWithGrape = await _context.Wines
                .Where(w => w.GrapeIds.Contains(1))
                .Take(10)
                .ToListAsync();
            stats["ArrayFilterTime"] = stopwatch.ElapsedMilliseconds;
            stats["GrapeFilterCount"] = winesWithGrape.Count;
            stopwatch.Reset();
            
            // Test text search performance
            stopwatch.Start();
            var winesWithText = await _context.Wines
                .FromSqlRaw(@"
                    SELECT w.* 
                    FROM ""Wines"" w
                    WHERE similarity(w.""Name"", 'red') > 0.3
                    LIMIT 50")
                .ToListAsync();
            stats["TextSearchTime"] = stopwatch.ElapsedMilliseconds;
            stats["TextSearchCount"] = winesWithText.Count;
            
            _logger.LogInformation("Database diagnostic stats generated");
            
            return View(stats);
        }
    }
}