using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using dotnetprojekt.Services;

namespace dotnetprojekt.Controllers
{
    public class WineController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<WineController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly BlobStorageService _blobStorageService;
        private readonly IExperienceLevelService _experienceLevelService;

        public WineController(WineLoversContext context, ILogger<WineController> logger, IWebHostEnvironment environment, BlobStorageService blobStorageService, IExperienceLevelService experienceLevelService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _blobStorageService = blobStorageService;
            _experienceLevelService = experienceLevelService;
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var wine = await _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Acidity)
                .Include(w => w.Ratings)
                    .ThenInclude(r => r.User)
                        .ThenInclude(u => u.ExperienceLevel)
                .Include(w => w.Winery)
                    .ThenInclude(winery => winery.Region)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wine == null)
            {
                return NotFound();
            }

            // Load related grapes
            if (wine.GrapeIds.Length > 0)
            {
                wine.Grapes = await _context.Grapes
                    .Where(g => wine.GrapeIds.Contains(g.Id))
                    .ToListAsync();
            }

            // Load paired dishes
            if (wine.PairWithIds.Length > 0)
            {
                wine.PairedDishes = await _context.Dishes
                    .Where(d => wine.PairWithIds.Contains(d.Id))
                    .ToListAsync();
            }

            return View(wine);
        }

        [Authorize]
        public async Task<IActionResult> PersonalRecommendations()
        {
            try
            {
                // Get current user ID
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    _logger.LogWarning("Failed to parse user ID when retrieving personal recommendations");
                    return RedirectToAction("Index", "Quiz");
                }

                // Get user preferences
                var preferences = await _context.UserPreferences
                    .Include(p => p.PreferredWineType)
                    .Include(p => p.PreferredAcidity)
                    .Include(p => p.PreferredCountry)
                    .Include(p => p.PreferredRegion)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (preferences == null)
                {
                    // User hasn't completed the quiz yet
                    TempData["Message"] = "Complete the Wine Quiz to get personalized recommendations.";
                    return RedirectToAction("Index", "Quiz");
                }

                // Get recommendations based on user preferences
                var recommendations = await GetPersonalizedRecommendations(preferences);

                // Create view model
                var viewModel = new PersonalRecommendationsViewModel
                {
                    Preferences = preferences,
                    Recommendations = recommendations,
                    LastUpdated = preferences.LastUpdated
                };


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal recommendations");
                TempData["ErrorMessage"] = "An error occurred while retrieving your recommendations.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult AddReview(int id)
        {
            // Check if wine exists
            var wine = _context.Wines.FirstOrDefault(w => w.Id == id);
            if (wine == null)
            {
                return NotFound();
            }

            var viewModel = new WineReviewViewModel
            {
                WineId = id
            };

            ViewBag.WineName = wine.Name;
            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(WineReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var wine = _context.Wines.FirstOrDefault(w => w.Id == model.WineId);
                ViewBag.WineName = wine?.Name;
                return View(model);
            }

            try
            {
                // Get current user ID
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    _logger.LogWarning("Failed to parse user ID when submitting wine review");
                    return RedirectToAction("Login", "Auth");
                }

                // Check if the user has already reviewed this wine
                var existingReview = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.WineId == model.WineId);

                if (existingReview != null)
                {
                    // Update existing review
                    existingReview.RatingValue = model.Rating;
                    existingReview.Description = model.Description;
                    existingReview.Date = DateTime.UtcNow;

                    // Process new image if provided
                    if (model.ReviewImage != null)
                    {
                        // Check if old image is from blob storage
                        if (!string.IsNullOrEmpty(existingReview.ImagePath))
                        {
                            // If the image is stored in Azure Blob Storage
                            if (existingReview.ImagePath.Contains("blob.core.windows.net") || existingReview.ImagePath.Contains("wineloversbucket"))
                            {
                                // Extract file name from the URL - remove SAS token
                                var oldFileName = BlobStorageService.ExtractBlobNameFromUrl(existingReview.ImagePath);
                                
                                try
                                {
                                    // Delete the blob
                                    await _blobStorageService.DeleteImageAsync(oldFileName);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Failed to delete blob {oldFileName}. Continuing with upload.");
                                }
                            }
                            // If the image is stored locally
                            else
                            {
                                var oldImagePath = Path.Combine(_environment.WebRootPath, existingReview.ImagePath.TrimStart('/'));
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }
                        }

                        // Save new image with error handling
                        try
                        {
                            existingReview.ImagePath = await SaveReviewImage(model.ReviewImage, userId, model.WineId);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ReviewImage", ex.Message);
                            var wine = _context.Wines.FirstOrDefault(w => w.Id == model.WineId);
                            ViewBag.WineName = wine?.Name;
                            return View(model);
                        }
                    }

                    _context.Update(existingReview);
                }
                else
                {
                    // Create new review
                    var newReview = new Rating
                    {
                        UserId = userId,
                        WineId = model.WineId,
                        RatingValue = model.Rating,
                        Description = model.Description,
                        Date = DateTime.UtcNow
                    };

                    // Process image if provided
                    if (model.ReviewImage != null)
                    {
                        try
                        {
                            // Save image and get URL with SAS token
                            newReview.ImagePath = await SaveReviewImage(model.ReviewImage, userId, model.WineId);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ReviewImage", ex.Message);
                            var wine = _context.Wines.FirstOrDefault(w => w.Id == model.WineId);
                            ViewBag.WineName = wine?.Name;
                            return View(model);
                        }
                    }

                    _context.Ratings.Add(newReview);
                }

                await _context.SaveChangesAsync();

                // updating user reviewcount and experience level
                await _experienceLevelService.UpdateExperienceLevelAsync(userId);

                TempData["SuccessMessage"] = "Your review has been submitted successfully!";
                return RedirectToAction("Details", new { id = model.WineId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting wine review");
                ModelState.AddModelError("", "An error occurred while submitting your review. Please try again.");
                var wine = _context.Wines.FirstOrDefault(w => w.Id == model.WineId);
                ViewBag.WineName = wine?.Name;
                return View(model);
            }
        }

        private async Task<string> SaveReviewImage(IFormFile image, int userId, int wineId)
        {
            // Validate file size
            if (image.Length > WineReviewViewModel.MaxFileSize)
            {
                throw new Exception("File size exceeds the maximum allowed (5MB)");
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!WineReviewViewModel.AllowedExtensions.Contains(fileExtension))
            {
                throw new Exception("Invalid file type. Only JPG, JPEG and PNG files are allowed");
            }

            // Create unique filename
            var uniqueFileName = $"review_{userId}_{wineId}_{DateTime.Now.Ticks}{fileExtension}";
            
            try
            {
                // Upload to Azure Blob Storage
                var blobUrl = await _blobStorageService.UploadImageAsync(image, uniqueFileName);
                _logger.LogInformation($"Image uploaded successfully to Azure Blob Storage: {blobUrl}");
                return blobUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image to Azure Blob Storage. Falling back to local storage.");
                
                // Fallback to local storage if Azure upload fails
                // Define upload directory
                var uploadDir = Path.Combine(_environment.WebRootPath, "images", "reviews");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Save file
                var filePath = Path.Combine(uploadDir, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Return relative path for database storage
                return $"/images/reviews/{uniqueFileName}";
            }
        }
        
        private async Task<List<Wine>> GetPersonalizedRecommendations(UserPreference preferences)
        {
            var query = _context.Wines
                .Include(w => w.Type)
                .Include(w => w.Country)
                .Include(w => w.Acidity)
                .Include(w => w.Ratings)
                .Include(w => w.Winery)
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

            // Apply country filter if specified
            if (preferences.PreferredCountryId.HasValue)
            {
                query = query.Where(w => w.CountryId == preferences.PreferredCountryId);
            }            // Apply region filter if specified - now filtering through winery
            if (preferences.PreferredRegionId.HasValue)
            {
                // Filter wines that have a winery in the preferred region
                query = query.Where(w => w.WineryId == preferences.PreferredRegionId );
            }

            // Apply dish pairing filter if specified
            if (preferences.PreferredDishIds != null && preferences.PreferredDishIds.Length > 0)
            {
                // Find wines that pair with at least one of the preferred dishes
                query = query.Where(w => preferences.PreferredDishIds.Any(d => w.PairWithIds.Contains(d)));
            }

            // Apply body preference if specified
            if (preferences.BodyPreference.HasValue)
            {
                // For simplicity, we'll use a heuristic based on ABV as a rough proxy for body
                // In a real app, we'd have a dedicated field for body
                decimal minAbv = 0, maxAbv = 20;
                
                switch (preferences.BodyPreference.Value)
                {
                    case 1: // Light
                        maxAbv = 11.5m;
                        break;
                    case 2: // Light-medium
                        minAbv = 10.0m;
                        maxAbv = 12.5m;
                        break;
                    case 3: // Medium
                        minAbv = 11.5m;
                        maxAbv = 13.5m;
                        break;
                    case 4: // Medium-full
                        minAbv = 12.5m;
                        maxAbv = 14.5m;
                        break;
                    case 5: // Full
                        minAbv = 13.5m;
                        break;
                }
                
                // Apply ABV filter as a proxy for body
                if (minAbv > 0)
                    query = query.Where(w => w.ABV >= minAbv);
                if (maxAbv < 20)
                    query = query.Where(w => w.ABV <= maxAbv);
            }

            // Apply flavor preferences if specified
            if (!string.IsNullOrEmpty(preferences.PreferredFlavors))
            {
                var flavors = preferences.PreferredFlavors.Split(',').Select(f => f.Trim().ToLower()).ToList();
                if (flavors.Any())
                {
                    // Use Elaborate field which contains wine descriptions
                    query = query.Where(w => flavors.Any(f => w.Elaborate.ToLower().Contains(f)));
                }
            }

            // Get top wines ordered by average rating
            return await query
                .OrderByDescending(w => w.Ratings.Any() ? w.Ratings.Average(r => r.RatingValue) : 0)
                .Take(10)
                .ToListAsync();
        }
    }
}