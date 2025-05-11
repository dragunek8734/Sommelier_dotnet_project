using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using dotnetprojekt.Context;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace dotnetprojekt.Controllers;

public class QuizController : Controller
{
    private readonly ILogger<QuizController> _logger;
    private readonly WineLoversContext _context;

    public QuizController(ILogger<QuizController> logger, WineLoversContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("User accessed the Wine Quiz page");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessQuiz(QuizViewModel model)
    {
        _logger.LogInformation("User submitted wine quiz answers");
        
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        // Get wine preferences based on quiz answers
        var preferences = DeterminePreferences(model);
        
        // Find matching wines
        var recommendations = await GetRecommendations(preferences);
        
        // Store only the wine IDs in TempData to avoid serialization issues
        if (recommendations != null && recommendations.Any())
        {
            TempData["RecommendationIds"] = string.Join(",", recommendations.Select(w => w.Id));

            // Added distinct countries ids and regions ids based on recommended wines
            //preferences.PreferredCountryId = recommendations
            //                                .Select(w => w.CountryId)
            //                                .Distinct()
            //                                .ToArray();

            var recommendedWineryIds = recommendations.Select(w => w.WineryId).Distinct().ToList();

            preferences.PreferredRegionIds = await _context.Wineries.Where(w => recommendedWineryIds.Contains(w.Id))
                                            .Select(w => w.RegionId)
                                            .Distinct()
                                            .ToListAsync();

        }
        
        // Save user preferences if the user is authenticated
        if (User.Identity.IsAuthenticated)
        {
            await SaveUserPreferences(model, preferences);
        }
        
        return RedirectToAction("Results");
    }

    public async Task<IActionResult> Results()
    {
        _logger.LogInformation("User viewing quiz results");
        
        // Get recommendation IDs from TempData
        var recommendationIdsString = TempData["RecommendationIds"] as string;
        
        if (string.IsNullOrEmpty(recommendationIdsString))
        {
            return RedirectToAction("Index");
        }
        
        // Parse the IDs and fetch the full wine details
        var recommendationIds = recommendationIdsString.Split(',').Select(int.Parse).ToList();
        
        var recommendations = await _context.Wines
            .Include(w => w.Type)
            .Include(w => w.Country)
            .Include(w => w.Acidity)
            .Include(w => w.Ratings)
            .Where(w => recommendationIds.Contains(w.Id))
            .ToListAsync();
        
        // If no recommendations found, go back to quiz
        if (recommendations == null || !recommendations.Any())
        {
            return RedirectToAction("Index");
        }
        
        // Reorder the recommendations to match the original order
        recommendations = recommendationIds
            .Select(id => recommendations.FirstOrDefault(w => w.Id == id))
            .Where(w => w != null)
            .ToList();
        
        return View(recommendations);
    }

    // New method to save user preferences to the database
    private async Task SaveUserPreferences(QuizViewModel model, QuizPreferences preferences)
    {
        try
        {
            // Get current user ID - try both JWT Sub claim and standard NameIdentifier
            var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub) 
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                
            if (!int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("Failed to parse user ID when saving quiz preferences");
                return;
            }

            // Check if the user already has preferences
            var userPreference = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userPreference == null)
            {
                // Create new preferences record
                userPreference = new UserPreference
                {
                    UserId = userId,
                    LastUpdated = DateTime.UtcNow
                };
                _context.UserPreferences.Add(userPreference);
            }
            else
            {
                // Update timestamp
                userPreference.LastUpdated = DateTime.UtcNow;
            }

            // Update preferences from quiz - mapping all available fields
            userPreference.PreferredWineTypeId = preferences.PreferredWineTypeId;
            userPreference.PreferredAcidityId = preferences.PreferredAcidityId;
            userPreference.PreferredRegionId = preferences.PreferredRegionId;
            userPreference.PreferredCountryId = preferences.PreferredCountryId;
            userPreference.BodyPreference = preferences.BodyPreference;
            userPreference.SweetnessPreference = model.SweetnessPreference;
            userPreference.PreferredDishIds = preferences.PreferredDishIds?.ToArray() ?? Array.Empty<int>();
            userPreference.PreferredFlavors = string.Join(",", preferences.PreferredFlavors ?? new List<string>());
            // Added occasion preference 20:45
            userPreference.Occasion = preferences.Occasion;
            userPreference.PrefferedRegions = preferences.PreferredRegionIds?.ToArray() ?? Array.Empty<int>();
            
            // Save changes
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Saved quiz preferences for user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user preferences from quiz");
        }
    }

    private QuizPreferences DeterminePreferences(QuizViewModel model)
    {
        var preferences = new QuizPreferences
        {
            PreferredWineTypeId = model.SweetnessPreference > 3 ? 2 : 1, // If sweetness > 3, prefer white wine over red
            PreferredFlavors = new List<string>(),
            PreferredCountryId = null, // Will be determined based on region or other preferences
            PreferredRegionId = null,   // Will be populated if region data is available
            PreferredRegionIds = new List<int>() // determine based of reccomendations
        };

        // Map food pairings to preferred dish types
        if (model.FoodPairings != null && model.FoodPairings.Any())
        {
            preferences.PreferredDishIds = new List<int>();
            foreach (var foodPairing in model.FoodPairings)
            {
                switch (foodPairing.ToLower())
                {
                    case "meat":
                        preferences.PreferredDishIds.Add(4); // Red meat dish ID origin 1 
                        preferences.PreferredDishIds.Add(12); // Game meat dish ID origin 5
                        preferences.PreferredDishIds.Add(10); // lamb id
                        preferences.PreferredDishIds.Add(24); // grilled meat dish id
                        break;
                    case "seafood":
                        preferences.PreferredDishIds.Add(26); // Seafood dish ID origin 2 
                        preferences.PreferredDishIds.Add(2); // rich fish id
                        preferences.PreferredDishIds.Add(32); // lean fish id
                        break;
                    case "pasta":
                        preferences.PreferredDishIds.Add(7); // Pasta dish ID origin 3
                        break;
                    case "dessert":
                        preferences.PreferredDishIds.Add(45); // Dessert dish ID origin 4
                        preferences.PreferredDishIds.Add(35); // cake dish id
                        preferences.PreferredDishIds.Add(36); // chocolate dish id
                        break;
                    case "poultry":
                        preferences.PreferredDishIds.Add(11); // poultry id
                        preferences.PreferredDishIds.Add(47); // duck dish id
                        preferences.PreferredDishIds.Add(31); // chicken dish id
                        break;
                    case "cheese":
                        preferences.PreferredDishIds.Add(9); // cheese id
                        break;
                }
            }
        }

        // Map flavor preferences
        if (model.FlavorPreference == "fruity")
        {
            preferences.PreferredFlavors.Add("berry");
            preferences.PreferredFlavors.Add("fruit");
            preferences.PreferredFlavors.Add("citrus");
        }
        else if (model.FlavorPreference == "earthy")
        {
            preferences.PreferredFlavors.Add("oak");
            preferences.PreferredFlavors.Add("earth");
            preferences.PreferredFlavors.Add("mineral");
        }
        else if (model.FlavorPreference == "spicy")
        {
            preferences.PreferredFlavors.Add("spice");
            preferences.PreferredFlavors.Add("pepper");
        }

        // Set acidity level based on user preference
        preferences.PreferredAcidityId = model.AcidityPreference;
        
        // Set body preference
        preferences.BodyPreference = model.BodyPreference;

        // Set Occasion preference  added 20:44
        preferences.Occasion = model.Occasion;
        
        return preferences;
    }

    private async Task<List<Wine>> GetRecommendations(QuizPreferences preferences)
    {
        // Start with a base query
        var query = _context.Wines
            .Include(w => w.Type)
            .Include(w => w.Country)
            .Include(w => w.Acidity)
            .AsQueryable();
        
        // Apply wine type filter if specified
        if (preferences.PreferredWineTypeId.HasValue)
        {
            query = query.Where(w => w.TypeId == preferences.PreferredWineTypeId.Value);
        }
        
        // Apply acidity filter if specified
        if (preferences.PreferredAcidityId.HasValue)
        {
            query = query.Where(w => w.AcidityId == preferences.PreferredAcidityId.Value);
        }
        
        // Apply dish pairing filter if specified
        if (preferences.PreferredDishIds != null && preferences.PreferredDishIds.Any())
        {
            // Find wines that pair with at least one of the preferred dishes
            query = query.Where(w => w.PairWithIds != null && preferences.PreferredDishIds.Any(d => w.PairWithIds.Contains(d)));
        }
        
        // TODO: In a real implementation, you would use a more sophisticated algorithm
        // that takes into account flavor preferences and body preference
        
        // Get top 5 wines ordered by average rating
        var recommendations = await query
            .OrderByDescending(w => w.Ratings.Any() ? w.Ratings.Average(r => r.RatingValue) : 0)
            .Take(5)
            .ToListAsync();
            
        return recommendations;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}