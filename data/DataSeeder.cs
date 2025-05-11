using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Data
{
    public class DataSeeder
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<DataSeeder> _logger;
        private readonly string _dataDirectory;
        private readonly string _metadataFilePath;
        private Dictionary<string, DateTime> _lastImportTimestamps = new Dictionary<string, DateTime>();
        private const int BATCH_SIZE = 10000; // Increased batch size for better performance

        public DataSeeder(WineLoversContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "data");
            _metadataFilePath = Path.Combine(_dataDirectory, "import_metadata.json");
            LoadImportMetadata();
        }

        private void LoadImportMetadata()
        {
            if (File.Exists(_metadataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_metadataFilePath);
                    _lastImportTimestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) 
                        ?? new Dictionary<string, DateTime>();
                    _logger.LogInformation("Loaded import metadata from file");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to load import metadata: {ex.Message}");
                    _lastImportTimestamps = new Dictionary<string, DateTime>();
                }
            }
        }

        private void SaveImportMetadata()
        {
            try
            {
                string json = JsonSerializer.Serialize(_lastImportTimestamps);
                File.WriteAllText(_metadataFilePath, json);
                _logger.LogInformation("Saved import metadata to file");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to save import metadata: {ex.Message}");
            }
        }

        private bool IsFileUpdated(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"File not found: {filePath}");
                return false;
            }

            DateTime lastModified = File.GetLastWriteTime(filePath);
            string fileName = Path.GetFileName(filePath);

            if (_lastImportTimestamps.TryGetValue(fileName, out DateTime lastImport))
            {
                // File is updated if it was modified after the last import
                return lastModified > lastImport;
            }

            // If no record of previous import, consider it as updated
            return true;
        }

        private void UpdateFileTimestamp(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            _lastImportTimestamps[fileName] = DateTime.Now;
        }        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding...");
                
                // Ensure database is created with the latest schema using migrations
                _logger.LogInformation("Ensuring database is created with the latest schema...");
                await _context.Database.MigrateAsync(); // Apply migrations instead of recreating the database
                
                // Seed reference data first
                _logger.LogInformation("[1/10] Seeding wine types...");
                await SeedWineTypesAsync();
                
                _logger.LogInformation("[2/10] Seeding wine acidities...");
                await SeedWineAciditiesAsync();
                
                _logger.LogInformation("[3/10] Seeding countries and regions...");
                await SeedCountriesAndRegionsAsync();
                
                _logger.LogInformation("[4/10] Seeding wineries...");
                await SeedWineriesAsync();
                
                _logger.LogInformation("[5/10] Seeding grapes...");
                await SeedGrapesAsync();
                
                _logger.LogInformation("[6/10] Seeding dishes...");
                await SeedDishesAsync();
                
                // Seed main data
                _logger.LogInformation("[7/10] Seeding wines...");
                await SeedWinesAsync();
                
                _logger.LogInformation("[8/10] Seeding users...");
                await SeedUsersAsync();
                
                _logger.LogInformation("[9/10] Seeding ratings...");
                await SeedRatingsAsync();
                
                _logger.LogInformation("[10/10] Data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during data seeding.");
                throw;
            }
        }

        private async Task SeedWineTypesAsync()
        {
            if (await _context.WineTypes.AnyAsync())
            {
                _logger.LogInformation("Wine types already seeded.");
                return;
            }

            var wineTypes = new List<WineType>
            {
                new WineType { Id = 1, Name = "Red" },
                new WineType { Id = 2, Name = "White" },
                new WineType { Id = 3, Name = "Rosé" },
                new WineType { Id = 4, Name = "Sparkling" },
                new WineType { Id = 5, Name = "Dessert" },
                new WineType { Id = 6, Name = "Fortified" }
            };

            await _context.WineTypes.AddRangeAsync(wineTypes);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Wine types seeded successfully.");
        }

        private async Task SeedWineAciditiesAsync()
        {
            if (await _context.WineAcidities.AnyAsync())
            {
                _logger.LogInformation("Wine acidities already seeded.");
                return;
            }

            var wineAcidities = new List<WineAcidity>
            {
                new WineAcidity { Id = 1, Name = "Low" },
                new WineAcidity { Id = 2, Name = "Medium" },
                new WineAcidity { Id = 3, Name = "High" }
            };

            await _context.WineAcidities.AddRangeAsync(wineAcidities);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Wine acidities seeded successfully.");
        }

        private async Task SeedCountriesAndRegionsAsync()
        {
            if (await _context.Countries.AnyAsync())
            {
                _logger.LogInformation("Countries already seeded.");
                return;
            }

            // We'll extract countries and regions from the wines CSV
            var winesFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_wines.csv");
            
            var countries = new Dictionary<string, Country>();
            var regions = new Dictionary<int, (string CountryCode, string RegionName)>();
            int recordCount = 0;
            int progressInterval = 1000;
            
            _logger.LogInformation("Reading countries and regions from CSV...");
            
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    recordCount++;
                    var countryCode = csv.GetField<string>("Code");
                    var countryName = csv.GetField<string>("Country");
                    var regionId = csv.GetField<int>("RegionID");
                    var regionName = csv.GetField<string>("RegionName");
                    
                    if (!string.IsNullOrEmpty(countryCode) && !countries.ContainsKey(countryCode))
                    {
                        countries.Add(countryCode, new Country
                        {
                            Code = countryCode,
                            Name = countryName ?? "Unknown"
                        });
                    }
                    
                    if (regionId > 0 && !regions.ContainsKey(regionId))
                    {
                        regions.Add(regionId, (countryCode ?? string.Empty, regionName ?? "Unknown"));
                    }
                    
                    if (recordCount % progressInterval == 0)
                    {
                        _logger.LogInformation($"Processed {recordCount} records for countries and regions...");
                    }
                }
            }
            
            // Add countries first
            _logger.LogInformation($"Adding {countries.Count} countries to database...");
            await _context.Countries.AddRangeAsync(countries.Values);
            await _context.SaveChangesAsync();
            
            // Get the saved countries with their IDs
            var savedCountries = await _context.Countries.ToDictionaryAsync(c => c.Code, c => c.Id);
            
            // Create region entities with the correct country IDs
            var regionEntities = new List<Region>();
            foreach (var (regionId, (countryCode, regionName)) in regions)
            {
                // Only add regions if we have a valid country ID
                if (!string.IsNullOrEmpty(countryCode) && savedCountries.TryGetValue(countryCode, out int countryId))
                {
                    regionEntities.Add(new Region
                    {
                        Id = regionId,
                        Name = regionName,
                        CountryId = countryId
                    });
                }
                else
                {
                    _logger.LogWarning($"Skipping region {regionId} ({regionName}) due to missing country reference");
                }
            }
            
            // Add regions
            _logger.LogInformation($"Adding {regionEntities.Count} regions to database...");
            await _context.Regions.AddRangeAsync(regionEntities);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Seeded {countries.Count} countries and {regionEntities.Count} regions.");
        }

        private async Task SeedWineriesAsync()
        {
            if (await _context.Wineries.AnyAsync())
            {
                _logger.LogInformation("Wineries already seeded.");
                return;
            }

            var winesFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_wines.csv");
            var wineries = new Dictionary<int, Winery>();
            int recordCount = 0;
            int progressInterval = 1000;
            
            _logger.LogInformation("Reading wineries from CSV...");
            
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    recordCount++;
                    var wineryId = csv.GetField<int>("WineryID");
                    var wineryName = csv.GetField<string>("WineryName");
                    var website = csv.GetField<string>("Website");
                    var regionId = csv.GetField<int>("RegionID");
                    
                    if (wineryId > 0 && !wineries.ContainsKey(wineryId))
                    {
                        wineries.Add(wineryId, new Winery
                        {
                            Id = wineryId,
                            Name = wineryName ?? "Unknown",
                            Website = website,
                            RegionId = regionId
                        });
                    }
                    
                    if (recordCount % progressInterval == 0)
                    {
                        _logger.LogInformation($"Processed {recordCount} records for wineries...");
                    }
                }
            }
            
            _logger.LogInformation($"Adding {wineries.Count} wineries to database...");
            await _context.Wineries.AddRangeAsync(wineries.Values);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Seeded {wineries.Count} wineries.");
        }

        private async Task SeedGrapesAsync()
        {
            if (await _context.Grapes.AnyAsync())
            {
                _logger.LogInformation("Grapes already seeded.");
                return;
            }

            var winesFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_wines.csv");
            var grapes = new HashSet<string>();
            int recordCount = 0;
            int progressInterval = 1000;
            
            _logger.LogInformation("Reading grapes from CSV...");
            
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    recordCount++;
                    var grapesStr = csv.GetField<string>("Grapes");
                    if (!string.IsNullOrEmpty(grapesStr))
                    {
                        // Parse the grape names from the string format ['Grape1', 'Grape2']
                        var matches = Regex.Matches(grapesStr, @"'([^']*)'");
                        foreach (Match match in matches)
                        {
                            grapes.Add(match.Groups[1].Value);
                        }
                    }
                    
                    if (recordCount % progressInterval == 0)
                    {
                        _logger.LogInformation($"Processed {recordCount} records for grapes... Found {grapes.Count} unique grapes so far");
                    }
                }
            }
            
            var grapeEntities = grapes.Select((grape, index) => new Grape
            {
                Id = index + 1,
                Name = grape
            }).ToList();
            
            _logger.LogInformation($"Adding {grapeEntities.Count} grapes to database...");
            await _context.Grapes.AddRangeAsync(grapeEntities);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Seeded {grapeEntities.Count} grapes.");
        }

        private async Task SeedDishesAsync()
        {
            if (await _context.Dishes.AnyAsync())
            {
                _logger.LogInformation("Dishes already seeded.");
                return;
            }

            var winesFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_wines.csv");
            var dishes = new HashSet<string>();
            int recordCount = 0;
            int progressInterval = 1000;
            
            _logger.LogInformation("Reading dishes from CSV...");
            
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    recordCount++;
                    var dishesStr = csv.GetField<string>("Harmonize");
                    if (!string.IsNullOrEmpty(dishesStr))
                    {
                        // Parse the dish names from the string format ['Dish1', 'Dish2']
                        var matches = Regex.Matches(dishesStr, @"'([^']*)'");
                        foreach (Match match in matches)
                        {
                            dishes.Add(match.Groups[1].Value);
                        }
                    }
                    
                    if (recordCount % progressInterval == 0)
                    {
                        _logger.LogInformation($"Processed {recordCount} records for dishes... Found {dishes.Count} unique dishes so far");
                    }
                }
            }
            
            var dishEntities = dishes.Select((dish, index) => new Dish
            {
                Id = index + 1,
                Name = dish
            }).ToList();
            
            _logger.LogInformation($"Adding {dishEntities.Count} dishes to database...");
            await _context.Dishes.AddRangeAsync(dishEntities);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Seeded {dishEntities.Count} dishes.");
        }        private async Task SeedWinesAsync()
        {
            var winesFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_wines.csv");
            
            // Check if wines table is already populated and file hasn't changed
            if (await _context.Wines.AnyAsync() && !IsFileUpdated(winesFilePath))
            {
                _logger.LogInformation("Wines already seeded and data file hasn't changed.");
                return;
            }
            
            // Only seed if no wines exist or file has been updated
            if (!await _context.Wines.AnyAsync() || IsFileUpdated(winesFilePath))
            {
                // Clear existing data only if we're updating
                if (await _context.Wines.AnyAsync())
                {
                    _logger.LogInformation("Clearing existing wines data for update...");
                    _context.Wines.RemoveRange(await _context.Wines.ToListAsync());
                    await _context.SaveChangesAsync();
                }var wines = new List<Wine>();
            // Dictionary to store wine-winery relationships for the one-to-many relationship
            var wineWineryRelationships = new Dictionary<int, int>();
            
            int totalRecords = 0;
            int processedRecords = 0;
            int batchSize = BATCH_SIZE; 
            int progressInterval = 100;
            
            // Count total records first for progress reporting
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                while (csv.Read())
                {
                    totalRecords++;
                }
            }
            
            _logger.LogInformation($"Found {totalRecords} wines to process...");
            
            // Load reference data
            _logger.LogInformation("Loading reference data for wines...");
            var grapeDict = await _context.Grapes.ToDictionaryAsync(g => g.Name, g => g.Id);
            var dishDict = await _context.Dishes.ToDictionaryAsync(d => d.Name, d => d.Id);
            var wineTypeDict = await _context.WineTypes.ToDictionaryAsync(t => t.Name, t => t.Id);
            var acidityDict = new Dictionary<string, int>
            {
                { "Low", 1 },
                { "Medium", 2 },
                { "High", 3 }
            };
            var countryDict = await _context.Countries.ToDictionaryAsync(c => c.Code, c => c.Id);
            var regionDict = await _context.Regions.ToDictionaryAsync(r => r.Id, r => r);
            _logger.LogInformation("Reference data loaded successfully.");
            
            using (var reader = new StreamReader(winesFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    processedRecords++;
                    var wineId = csv.GetField<int>("WineID");
                    var wineName = csv.GetField<string>("WineName");
                    var wineType = csv.GetField<string>("Type");
                    var elaborate = csv.GetField<string>("Elaborate");
                    var grapesStr = csv.GetField<string>("Grapes");
                    var dishesStr = csv.GetField<string>("Harmonize");
                    var abvStr = csv.GetField<string>("ABV");
                    var acidity = csv.GetField<string>("Acidity");
                    var countryCode = csv.GetField<string>("Code");
                    var regionId = csv.GetField<int>("RegionID");
                    var wineryId = csv.GetField<int>("WineryID");
                    var vintagesStr = csv.GetField<string>("Vintages");
                      // Store the wine-winery relationship for later use
                    if (wineryId > 0)
                    {
                        // In a one-to-many relationship, each wine has only one winery
                        wineWineryRelationships[wineId] = wineryId;
                    }
                    
                    // Parse ABV
                    decimal abvValue = 0;
                    decimal.TryParse(abvStr, out abvValue);
                    // Store as decimal percentage (e.g., 12.5 for 12.5%)
                    
                    // Parse grape IDs
                    var grapeIds = new List<int>();
                    if (!string.IsNullOrEmpty(grapesStr))
                    {
                        var matches = Regex.Matches(grapesStr, @"'([^']*)'");
                        foreach (Match match in matches)
                        {
                            var grapeName = match.Groups[1].Value;
                            if (!string.IsNullOrEmpty(grapeName) && grapeDict != null && grapeDict.TryGetValue(grapeName, out int grapeId))
                            {
                                grapeIds.Add(grapeId);
                            }
                        }
                    }
                    
                    // Parse dish IDs
                    var dishIds = new List<int>();
                    if (!string.IsNullOrEmpty(dishesStr))
                    {
                        var matches = Regex.Matches(dishesStr, @"'([^']*)'");
                        foreach (Match match in matches)
                        {
                            var dishName = match.Groups[1].Value;
                            if (dishDict != null && dishDict.TryGetValue(dishName, out int dishId))
                            {
                                dishIds.Add(dishId);
                            }
                        }
                    }
                    
                    // Parse vintages
                    var vintages = new List<string>();
                    if (!string.IsNullOrEmpty(vintagesStr))
                    {
                        var matches = Regex.Matches(vintagesStr, @"(\d{4}|'N\.V\.')");
                        foreach (Match match in matches)
                        {
                            vintages.Add(match.Groups[1].Value.Replace("'", ""));
                        }
                    }
                      // Get type ID
                    int typeId = 1; // Default to Red
                    if (wineTypeDict != null && !string.IsNullOrEmpty(wineType) && wineTypeDict.TryGetValue(wineType, out int foundTypeId))
                    {
                        typeId = foundTypeId;
                    }
                    
                    // Get acidity ID
                    int acidityId = 2; // Default to Medium
                    if (acidity != null && acidityDict.TryGetValue(acidity.Split('-')[0], out int foundAcidityId))
                    {
                        acidityId = foundAcidityId;
                    }
                    
                    // Get country ID
                    int countryId = 1; // Default to first country
                    if (!string.IsNullOrEmpty(countryCode) && countryDict != null && countryDict.TryGetValue(countryCode, out int foundCountryId))
                    {
                        countryId = foundCountryId;
                    }
                          var wine = new Wine
                    {
                        Id = wineId,
                        Name = wineName ?? "Unknown Wine",
                        TypeId = typeId,
                        Elaborate = elaborate ?? "",
                        GrapeIds = grapeIds.ToArray(),
                        PairWithIds = dishIds.ToArray(),
                        ABV = abvValue,
                        AcidityId = acidityId,
                        CountryId = countryId,
                        Vintages = vintages.ToArray(),
                        WineryId = wineryId > 0 ? wineryId : null // Set winery directly on the wine
                    };
                    
                    wines.Add(wine);
                    
                    // Process in batches to avoid memory issues
                    if (wines.Count >= batchSize)
                    {
                        await _context.Wines.AddRangeAsync(wines);
                        await _context.SaveChangesAsync();
                        
                        double progressPercentage = (double)processedRecords / totalRecords * 100;
                        _logger.LogInformation($"Wine import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%)");
                        
                        wines.Clear();
                    }
                    else if (processedRecords % progressInterval == 0)
                    {
                        double progressPercentage = (double)processedRecords / totalRecords * 100;
                        _logger.LogInformation($"Wine import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%)");
                    }
                }
                
                // Save any remaining wines
                if (wines.Any())
                {
                    await _context.Wines.AddRangeAsync(wines);
                    await _context.SaveChangesAsync();
                    
                    double progressPercentage = (double)processedRecords / totalRecords * 100;
                    _logger.LogInformation($"Wine import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%)");
                }
            }
              // The wine-winery relationships are now set directly in the Wine objects
            // No need to set up many-to-many relationships as we've moved to one-to-many
            _logger.LogInformation("Wine-winery relationships established through WineryId.");
            
            // We can verify and log how many wines have wineries assigned
            var winesWithWinery = await _context.Wines.Where(w => w.WineryId != null).CountAsync();
            _logger.LogInformation($"{winesWithWinery} wines have been associated with a winery.");
            
            // We can also ensure all WineryId values reference valid wineries
            var wineIdsWithInvalidWinery = await _context.Wines
                .Where(w => w.WineryId != null && !_context.Wineries.Any(wn => wn.Id == w.WineryId))
                .Select(w => w.Id)
                .ToListAsync();
            
            if (wineIdsWithInvalidWinery.Any())
            {
                _logger.LogWarning($"Found {wineIdsWithInvalidWinery.Count} wines with invalid winery references. Fixing...");
                foreach (var invalidWineId in wineIdsWithInvalidWinery)
                {
                    var wine = await _context.Wines.FindAsync(invalidWineId);
                    if (wine != null)
                    {
                        wine.WineryId = null;
                    }
                }
                await _context.SaveChangesAsync();            _logger.LogInformation("Fixed invalid winery references.");
            }
            
            _logger.LogInformation("Wines seeded successfully.");
            UpdateFileTimestamp(winesFilePath);
            }  // Close the "if (!await _context.Wines.AnyAsync() || IsFileUpdated(winesFilePath))" block
        }        private async Task SeedUsersAsync()
        {
            var ratingsFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_ratings.csv");
            
            // Check if users table is already populated and file hasn't changed
            if (await _context.Users.AnyAsync() && !IsFileUpdated(ratingsFilePath))
            {
                _logger.LogInformation("Users already seeded and data file hasn't changed.");
                return;
            }
            
            // Only seed if no users exist or file has been updated
            if (!await _context.Users.AnyAsync() || IsFileUpdated(ratingsFilePath))
            {
                // If we need to update, clear existing data first
                if (await _context.Users.AnyAsync())
                {
                    _logger.LogInformation("Clearing existing users data for update...");
                    _context.Users.RemoveRange(await _context.Users.ToListAsync());
                    await _context.SaveChangesAsync();
                }

            // Create a set of sample users based on the ratings file
            var userIds = new HashSet<int>();
            int recordCount = 0;
            int progressInterval = 1000;
            
            _logger.LogInformation("Reading users from ratings CSV...");
            
            using (var reader = new StreamReader(ratingsFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    recordCount++;
                    var userId = csv.GetField<int>("UserID");
                    userIds.Add(userId);
                    
                    if (recordCount % progressInterval == 0)
                    {
                        _logger.LogInformation($"Processed {recordCount} records for users... Found {userIds.Count} unique users so far");
                    }
                }
            }
            
            // Create users in batches to improve performance
            var totalUsers = userIds.Count;
            _logger.LogInformation($"Preparing to add {totalUsers} users to database...");
            
            int batchSize = 1000; // Smaller batch size for users to avoid excessive memory usage
            int processed = 0;
            
            // Process userIds in batches
            foreach (var userIdBatch in userIds.Chunk(batchSize))
            {
                var userBatch = userIdBatch.Select(id => new User
                {
                    Id = id,
                    Username = $"user{id}",
                    Email = $"user{id}@example.com",
                    Password = "password123" // Properly hash passwords for security
                }).ToList();
                
                await _context.Users.AddRangeAsync(userBatch);
                await _context.SaveChangesAsync();
                
                processed += userBatch.Count;
                _logger.LogInformation($"Added users batch: {processed}/{totalUsers} ({(double)processed/totalUsers*100:F2}%)");
            }
              _logger.LogInformation($"Seeded {totalUsers} users successfully.");
            UpdateFileTimestamp(ratingsFilePath);
            } // Close the "if (!await _context.Users.AnyAsync() || IsFileUpdated(ratingsFilePath))" block
        }        private async Task SeedRatingsAsync()
        {
            var ratingsFilePath = Path.Combine(_dataDirectory, "XWines_Filtered_ratings.csv");
            
            // Check if ratings table is already populated and file hasn't changed
            if (await _context.Ratings.AnyAsync() && !IsFileUpdated(ratingsFilePath))
            {
                _logger.LogInformation("Ratings already seeded and data file hasn't changed.");
                return;
            }
            
            // Only seed if no ratings exist or file has been updated
            if (!await _context.Ratings.AnyAsync() || IsFileUpdated(ratingsFilePath))
            {
                // If we need to update, clear existing data first
                if (await _context.Ratings.AnyAsync())
                {
                    _logger.LogInformation("Clearing existing ratings data for update...");
                    _context.Ratings.RemoveRange(await _context.Ratings.ToListAsync());
                    await _context.SaveChangesAsync();
                }

            var ratings = new List<Rating>();
            int totalRecords = 0;
            int processedRecords = 0;
            int validRecords = 0;
            int batchSize = BATCH_SIZE; 
            int progressInterval = 1000;
            
            // Count total records first for progress reporting
            using (var reader = new StreamReader(ratingsFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                while (csv.Read())
                {
                    totalRecords++;
                }
            }
            
            _logger.LogInformation($"Found {totalRecords} ratings to process...");
            
            // Get the list of valid user IDs and wine IDs
            _logger.LogInformation("Loading valid user and wine IDs...");
            var validUserIds = await _context.Users.Select(u => u.Id).ToHashSetAsync();
            var validWineIds = await _context.Wines.Select(w => w.Id).ToHashSetAsync();
            _logger.LogInformation($"Found {validUserIds.Count} valid users and {validWineIds.Count} valid wines.");
            
            using (var reader = new StreamReader(ratingsFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.Read(); // Skip header
                csv.ReadHeader();
                
                while (csv.Read())
                {
                    processedRecords++;
                    
                    try
                    {
                        var ratingId = csv.GetField<int>("RatingID");
                        var userId = csv.GetField<int>("UserID");
                        var wineId = csv.GetField<int>("WineID");
                        var ratingValue = csv.GetField<decimal>("Rating");
                        var dateStr = csv.GetField<string>("Date");
                        
                        // Skip if user or wine doesn't exist in our database
                        if (!validUserIds.Contains(userId) || !validWineIds.Contains(wineId))
                            continue;
                        
                        DateTime date;
                        if (!DateTime.TryParse(dateStr, out date))
                        {
                            date = DateTime.Now.AddDays(-processedRecords % 365); // Fallback
                        }
                        
                        // Ensure DateTime is in UTC format for PostgreSQL
                        date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                        
                        var rating = new Rating
                        {
                            Id = ratingId,
                            UserId = userId,
                            WineId = wineId,
                            RatingValue = ratingValue,
                            Date = date
                        };
                        
                        ratings.Add(rating);
                        validRecords++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error processing rating at record {processedRecords}: {ex.Message}");
                        continue; // Skip this record and continue
                    }
                    
                    // Process in batches to avoid memory issues
                    if (ratings.Count >= batchSize)
                    {
                        try
                        {
                            await _context.Ratings.AddRangeAsync(ratings);
                            await _context.SaveChangesAsync();
                            
                            double progressPercentage = (double)processedRecords / totalRecords * 100;
                            _logger.LogInformation($"Rating import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%) - Valid ratings: {validRecords}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error saving batch of ratings: {ex.Message}");
                            // Try to save one by one to identify problematic records
                            foreach (var rating in ratings)
                            {
                                try
                                {
                                    await _context.Ratings.AddAsync(rating);
                                    await _context.SaveChangesAsync();
                                }
                                catch (Exception innerEx)
                                {
                                    _logger.LogWarning($"Failed to save rating {rating.Id}: {innerEx.Message}");
                                }
                            }
                        }
                        
                        ratings.Clear();
                    }
                    else if (processedRecords % progressInterval == 0)
                    {
                        double progressPercentage = (double)processedRecords / totalRecords * 100;
                        _logger.LogInformation($"Rating import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%) - Valid ratings: {validRecords}");
                    }
                }
                
                // Save any remaining ratings
                if (ratings.Any())
                {
                    try
                    {
                        await _context.Ratings.AddRangeAsync(ratings);
                        await _context.SaveChangesAsync();
                        
                        double progressPercentage = (double)processedRecords / totalRecords * 100;
                        _logger.LogInformation($"Rating import progress: {processedRecords}/{totalRecords} ({progressPercentage:F2}%) - Valid ratings: {validRecords}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error saving final batch of ratings: {ex.Message}");
                        // Try to save one by one to identify problematic records
                        foreach (var rating in ratings)
                        {
                            try
                            {
                                await _context.Ratings.AddAsync(rating);
                                await _context.SaveChangesAsync();
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogWarning($"Failed to save rating {rating.Id}: {innerEx.Message}");
                            }
                        }
                    }
                }
            }
              _logger.LogInformation($"Ratings seeded successfully. Imported {validRecords} valid ratings out of {processedRecords} processed records.");
            UpdateFileTimestamp(ratingsFilePath);
            } // Close the "if (!await _context.Ratings.AnyAsync() || IsFileUpdated(ratingsFilePath))" block
        }
    }
}
