using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using dotnetprojekt.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace dotnetprojekt.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly BlobStorageService _blobStorageService;
        private readonly IExperienceLevelService _experienceLevelService;

        public AccountController(WineLoversContext context, ILogger<AccountController> logger, 
            IWebHostEnvironment webHostEnvironment, BlobStorageService blobStorageService, IExperienceLevelService experienceLevelService)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _blobStorageService = blobStorageService;
            _experienceLevelService = experienceLevelService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get user ID from JWT
            var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
            if (!int.TryParse(subClaim, out int userId))
                return Unauthorized();

            // Load user with related entities
            var user = await _context.Users
                .Include(u => u.Ratings)
                    .ThenInclude(r => r.Wine)
                .Include(e => e.ExperienceLevel)
                .Include(u => u.LoginHistory)
                //.Include(u => u.ExperienceLevel)              
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Load user preferences
            var preferences = await _context.UserPreferences
                .Include(p => p.PreferredWineType)
                .Include(p => p.PreferredAcidity)
                .Include(p => p.PreferredCountry)   // modified
                .Include(p => p.PreferredRegion)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Fetch real login history
            var loginHistory = await _context.LoginHistory
                .Where(lh => lh.UserId == userId)
                .OrderByDescending(lh => lh.LoginTime)
                .Take(10) // Limit to last 10 login attempts
                .Select(lh => new LoginHistoryViewModel
                {
                    Date = lh.LoginTime,
                    IpAddress = lh.IpAddress,
                    Device = lh.Device,
                    Success = lh.Success
                })
                .ToListAsync();

            // For demo: mock notification and privacy settings (replace with real if available)
            var notificationSettings = new NotificationSettingsViewModel
            {
                Email = true,
                Push = false,
                Sms = false
            };
            
            var privacySettings = new PrivacySettingsViewModel
            {
                ProfileVisible = true,
                TastingHistoryVisible = false
            };

            // Map ratings to view model
            var ratings = user.Ratings?.Select(r => new RatingViewModel
            {
                Id = r.Id,
                WineId = r.WineId,
                WineName = r.Wine?.Name ?? string.Empty,
                RatingValue = r.RatingValue,
                Description = r.Description,
                ImagePath = r.ImagePath,
                Date = r.Date
            }).OrderByDescending(r => r.Date).ToList() ?? new List<RatingViewModel>();

            // Get dishes added 21:02

            var dishes = new List<string>();
            var regions = new List<string>();

            if (preferences?.PreferredDishIds != null && preferences?.PrefferedRegions != null)
            {
                dishes = await _context.Dishes.Where(d => preferences.PreferredDishIds.Contains(d.Id))
                                                .Select(d => d.Name)
                                                .ToListAsync();

                regions = await _context.Regions.Where(r => preferences.PrefferedRegions.Contains(r.Id))
                                                    .Select(r => r.Name)
                                                    .ToListAsync();
            }
           



                // Create user preferences view model
                var preferencesViewModel = new UserPreferencesViewModel();
            if (preferences != null)
            {
                // added types as preferred wine type / sweetness / acidity
                var favoriteWineTypes = new List<string>();

                if (!string.IsNullOrWhiteSpace(preferences.PreferredWineType?.Name))
                    favoriteWineTypes.Add(preferences.PreferredWineType.Name);

                if (preferences.SweetnessPreference.HasValue)
                    favoriteWineTypes.Add(GetSweetness(preferences.SweetnessPreference.Value));

                if (!string.IsNullOrWhiteSpace(preferences.PreferredAcidity?.Name))
                    favoriteWineTypes.Add(preferences.PreferredAcidity.Name + " acidity");



                preferencesViewModel = new UserPreferencesViewModel
                {
                    FavoriteWineType = preferences.PreferredWineType?.Name ?? string.Empty,
                    
                    FavoriteRegion = preferences.PreferredRegion?.Name ?? string.Empty,  
                    FavoriteCountry = preferences.PreferredCountry?.Name ?? string.Empty,
                    Sweetness = preferences.SweetnessPreference,
                    Body = preferences.BodyPreference,
                    Acidity = preferences.PreferredAcidity?.Name ?? string.Empty,
                    Tannin = preferences.TanninPreference,
                    AbvMin = preferences.PreferredAbvMin,
                    AbvMax = preferences.PreferredAbvMax,
                    Flavors = preferences.PreferredFlavors ?? string.Empty,
                    FoodPairings = preferences.PreferredDishIds ?? Array.Empty<int>(),
                    LastUpdated = preferences.LastUpdated,
                    // Additional display properties
                    Experience = user.ExperienceLevel?.Name ?? "Beginner",            // odkomentowac
                    FavoriteWineTypes = favoriteWineTypes,
                    FavoriteDishes = dishes  ?? new List<string> { "beef", "seafood"},
                    FavoriteRegions = regions ?? new List<string> { "Bordeaux", "Tuscany" },
                    TypicalOccasions = new List<string> { preferences.Occasion ?? string.Empty, "Special Occasions" }
                };
            }
            else
            {
                // Add sample data when no preferences exist
                preferencesViewModel = new UserPreferencesViewModel 
                {
                    Experience = "Beginner",
                    FavoriteWineTypes = new List<string> { "Red" },
                    FavoriteRegions = new List<string> { "California" },
                    TypicalOccasions = new List<string> { "Weekends" }
                };
            }

            // Build the final view model
            var model = new UserAccountViewModel
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                ProfileImageUrl = !string.IsNullOrEmpty(user.ProfileImageUrl) ? user.ProfileImageUrl : "/img/default-avatar.png",
                MemberSince = user.CreatedAt ?? DateTime.UtcNow.AddMonths(-6),
                Ratings = ratings,
                Preferences = preferencesViewModel,
                NotificationSettings = notificationSettings,
                PrivacySettings = privacySettings,
                LoginHistory = loginHistory
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // Get user ID from claims
            var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
            if (!int.TryParse(subClaim, out int userId))
                return Unauthorized();
                
            // Print claims for debugging
            _logger.LogInformation("Claims: " + string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));

            // Load user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Create edit view model
            var model = new UserAccountEditViewModel
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                ProfileImageUrl = !string.IsNullOrEmpty(user.ProfileImageUrl) ? user.ProfileImageUrl : "/img/default-avatar.png"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserAccountEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Get user ID from claims
            var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
            if (!int.TryParse(subClaim, out int userId))
                return Unauthorized();
            
            // Update the model's UserId with the authenticated user's ID
            model.UserId = userId;

            // Load user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // Handle profile image upload
            if (model.ProfileImage != null)
            {
                // Check file size
                if (model.ProfileImage.Length > UserAccountEditViewModel.MaxFileSize)
                {
                    ModelState.AddModelError("ProfileImage", $"Image size exceeds the {UserAccountEditViewModel.MaxFileSize / (1024 * 1024)}MB limit.");
                    return View(model);
                }

                // Check file extension
                var extension = Path.GetExtension(model.ProfileImage.FileName).ToLowerInvariant();
                if (!UserAccountEditViewModel.AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ProfileImage", "Only JPG, JPEG, and PNG files are allowed.");
                    return View(model);
                }

                try
                {
                    // Check if user already has a profile image
                    if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                    {
                        // Extract the filename from the URL
                        string existingBlobName = BlobStorageService.ExtractBlobNameFromUrl(user.ProfileImageUrl);
                        
                        // Delete the existing blob if it exists
                        if (!string.IsNullOrEmpty(existingBlobName))
                        {
                            await _blobStorageService.DeleteImageAsync(existingBlobName, isProfileImage: true);
                        }
                    }

                    // Generate unique filename
                    var fileName = $"user_{userId}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}{extension}";
                    
                    // Upload to blob storage
                    string blobUrl = await _blobStorageService.UploadImageAsync(model.ProfileImage, fileName, isProfileImage: true);

                    // Update user with new profile image URL (SAS token URL)
                    user.ProfileImageUrl = blobUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading profile image to blob storage");
                    ModelState.AddModelError("", "Error uploading profile image. Please try again.");
                    return View(model);
                }
            }

            // Update user information
            user.Username = model.Username;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Bio = model.Bio;

            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                ModelState.AddModelError("", "Error updating profile. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Edit));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "The new password and confirmation password do not match.";
                return RedirectToAction(nameof(Edit));
            }

            // Get user ID from claims
            var subClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
            if (!int.TryParse(subClaim, out int userId))
                return Unauthorized();

            // Load user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound();

            // In a real application, you would verify the current password and hash the new password
            // For demo purposes, we'll just update it directly

            // TODO: Add proper password verification and hashing
            
            try
            {
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                TempData["ErrorMessage"] = "Error changing password. Please try again.";
                return RedirectToAction(nameof(Edit));
            }
        }

        public string GetSweetness(int val)
        {
            switch (val)
            {
                case 1:
                    return "Bone Dry";
                case 2:
                    return "Dry";
                case 3:
                    return "Semi-sweet";
                case 4:
                    return "Sweet";
                case 5:
                    return "Very Sweet";
                default:
                    return string.Empty;
            }
        }


        public async Task<IActionResult> DeleteReview(int id)
        {

            var review = await _context.Ratings.FindAsync(id);

            
            if (review == null) return NotFound();

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                _logger.LogWarning("Failed to parse user ID when deleting wine review");
                return RedirectToAction("Login", "Auth");
            }

            _context.Ratings.Remove(review);
            await _context.SaveChangesAsync();

            await _experienceLevelService.UpdateExperienceLevelAsync(userId);

            ViewData["active"] = "reviews";
            return RedirectToAction("Index","Account");

        }

        [HttpGet]
        public async Task<IActionResult> EditReview(int id)
        {
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null)
                return NotFound();

            var wine = await _context.Wines.FirstOrDefaultAsync(w => w.Id == rating.WineId);


            var viewModel = new WineReviewEditViewModel
            {
                Id = rating.Id,
                WineId = rating.WineId,
                WineName = wine?.Name ?? string.Empty,
                RatingValue = rating.RatingValue,
                Description = rating.Description ?? string.Empty,
                ImagePath = rating.ImagePath ,
                Date = rating.Date
            };

            return View(viewModel);

        }

        [HttpPost]
        public async Task<IActionResult> EditReview(WineReviewEditViewModel model, bool RemoveImage = false)
        {
            var review = await _context.Ratings.FindAsync(model.Id);

            if (review == null)
                return NotFound();

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                _logger.LogWarning("Failed to parse user ID when editing wine review");
                return RedirectToAction("Login", "Auth");
            }

            review.RatingValue = model.RatingValue;
            review.Description = model.Description;
            review.Date = DateTime.UtcNow;

            if(RemoveImage )
            {
                if(!string.IsNullOrEmpty(review.ImagePath))
                {
                    await DeleteOldImage(review.ImagePath);
                    review.ImagePath = string.Empty;
                }
            }
            else if(model.ReviewImage != null)
            {
                if (!string.IsNullOrEmpty(review.ImagePath))
                {
                    await DeleteOldImage(review.ImagePath);
                    review.ImagePath = string.Empty;
                }
                // Save new image with error handling
                try
                {
                    review.ImagePath = await SaveReviewImage(model.ReviewImage, userId, model.WineId);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ReviewImage", ex.Message);
                    var wine = _context.Wines.FirstOrDefault(w => w.Id == model.WineId);
                    ViewBag.WineName = wine?.Name;
                    return View(model);
                }
            }
            

            await _context.SaveChangesAsync();

            ViewData["active"] = "reviews";
            return RedirectToAction("Index", "Account");


        }

        private async Task DeleteOldImage(string oldPath)
        {
            // Check if old image is from blob storage
            if (!string.IsNullOrEmpty(oldPath))
            {
                // If the image is stored in Azure Blob Storage
                if (oldPath.Contains("blob.core.windows.net") || oldPath.Contains("wineloversbucket"))
                {
                    // Extract file name from the URL - remove SAS token
                    var oldFileName = BlobStorageService.ExtractBlobNameFromUrl(oldPath);

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
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                        
                    }
                }
            }
        }
        private async Task<string> SaveReviewImage(IFormFile image, int userId, int wineId)
        {
            // Validate file size
            if (image.Length > WineReviewEditViewModel.MaxFileSize)
            {
                throw new Exception("File size exceeds the maximum allowed (5MB)");
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!WineReviewEditViewModel.AllowedExtensions.Contains(fileExtension))
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
                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "reviews");

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






    }




}