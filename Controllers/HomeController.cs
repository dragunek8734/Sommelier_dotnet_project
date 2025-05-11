using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using dotnetprojekt.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace dotnetprojekt.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly WineLoversContext _context;

    public HomeController(ILogger<HomeController> logger, WineLoversContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        List<Wine> topRatedWines;
        
        try
        {
            // Try to get top 3 wine IDs from the materialized view
            var topRatedWineIds = _context.TopRatedWines
                .Take(3)
                .Select(t => t.WineId)
                .ToList();

            // Then fetch the complete wine entities with their related data
            topRatedWines = _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Ratings)
                .Where(w => topRatedWineIds.Contains(w.Id))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accessing materialized view, falling back to direct query");
            
            // Fallback to the original query if materialized view doesn't exist
            topRatedWines = _context.Wines
                .Include(w => w.Ratings)
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Where(w => w.Ratings.Count > 0)
                .Select(w => new
                {
                    Wine = w,
                    AverageRating = w.Ratings.Average(r => r.RatingValue)
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(3)
                .Select(x => x.Wine)
                .ToList();
                
            // Try to create the materialized view for future requests
            try
            {
                _context.Database.ExecuteSqlRaw(@"
                    CREATE MATERIALIZED VIEW IF NOT EXISTS mv_top_rated_wines AS
                    SELECT 
                        w.""Id"" AS wine_id, 
                        w.""Name"" AS name, 
                        AVG(r.""RatingValue"")::numeric(10,2) AS average_rating
                    FROM public.""Wines"" w
                    JOIN public.""Ratings"" r ON w.""Id"" = r.""WineId""
                    WHERE w.""Id"" IS NOT NULL
                    GROUP BY w.""Id"", w.""Name""
                    ORDER BY AVG(r.""RatingValue"") DESC
                    LIMIT 20");
                    
                _context.Database.ExecuteSqlRaw("REFRESH MATERIALIZED VIEW mv_top_rated_wines");
                
                _logger.LogInformation("Successfully created materialized view mv_top_rated_wines");
            }
            catch (Exception viewEx)
            {
                _logger.LogError(viewEx, "Failed to create materialized view");
            }
        }

        return View(topRatedWines);
    }

    
    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
