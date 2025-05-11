using System.Threading.Tasks;
using dotnetprojekt.Models;
using dotnetprojekt.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using dotnetprojekt.Context;
using System.Linq;

namespace dotnetprojekt.Controllers
{
    public class SearchController : Controller
    {
        private readonly SearchService _searchService;
        private readonly WineLoversContext _context;

        // Constants for default empty filter data
        private const string ALL_OPTION_TEXT = "Any";
        private const int ALL_OPTION_VALUE = -1;

        public SearchController(SearchService searchService, WineLoversContext context)
        {
            _searchService = searchService;
            _context = context;
        }

        // Helper method to create a base wine query with all filters EXCEPT the excluded one
        private IQueryable<Wine> CreateFilteredWineQuery(
            string query = null,
            int? typeId = null, 
            int? countryId = null, 
            int? regionId = null,
            int? grapeId = null,
            int? wineryId = null,
            int? acidityId = null, 
            decimal? minAbv = null, 
            decimal? maxAbv = null,
            int? minVintage = null,
            int? maxVintage = null,
            string excludeFilter = null)
        {
            var wineQuery = _context.Wines.AsQueryable();
            
            // Apply each filter unless it's the one we're excluding
            if (excludeFilter != "type" && typeId.HasValue)
                wineQuery = wineQuery.Where(w => w.TypeId == typeId.Value);
            
            if (excludeFilter != "country" && countryId.HasValue)
                wineQuery = wineQuery.Where(w => w.CountryId == countryId.Value);
            
            if (excludeFilter != "acidity" && acidityId.HasValue)
                wineQuery = wineQuery.Where(w => w.AcidityId == acidityId.Value);
            
            if (excludeFilter != "abv" && minAbv.HasValue)
                wineQuery = wineQuery.Where(w => w.ABV >= minAbv.Value);
            
            if (excludeFilter != "abv" && maxAbv.HasValue)
                wineQuery = wineQuery.Where(w => w.ABV <= maxAbv.Value);
            
            if (excludeFilter != "grape" && grapeId.HasValue)
                wineQuery = wineQuery.Where(w => w.GrapeIds.Contains(grapeId.Value));
                
            // Handle region filter through wineries (unless we're excluding regions)
            if (excludeFilter != "region" && regionId.HasValue)
            {
                // This is async but we can use the sync version for LINQ query building
                var wineryIds = _context.Wineries
                    .Where(w => w.RegionId == regionId.Value)
                    .Select(w => w.Id)
                    .ToList();
                
                if (wineryIds.Any())
                    wineQuery = wineQuery.Where(w => wineryIds.Contains(w.WineryId ?? 0));
            }
            
            // Apply winery filter directly (unless we're excluding wineries)
            if (excludeFilter != "winery" && wineryId.HasValue)
                wineQuery = wineQuery.Where(w => w.WineryId == wineryId.Value);
                
            // Apply text search if provided (unless we're excluding it)
            if (excludeFilter != "query" && !string.IsNullOrWhiteSpace(query))
            {
                string[] searchWords = query.Split(new char[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var word in searchWords)
                {
                    string searchTerm = word.ToLower();
                    wineQuery = wineQuery.Where(w => 
                        EF.Functions.ILike(w.Name, $"%{searchTerm}%") || 
                        EF.Functions.ILike(w.Elaborate, $"%{searchTerm}%"));
                }
            }
            
            return wineQuery;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string query, 
            int? typeId = null, 
            int? countryId = null, 
            int? regionId = null,
            int? grapeId = null,
            int? wineryId = null,
            int? acidityId = null, 
            decimal? minAbv = null, 
            decimal? maxAbv = null,
            int? minVintage = null,
            int? maxVintage = null,
            SearchService.SortOption sort = SearchService.SortOption.Relevance)
        {
            ViewData["Query"] = query;
            
            // Check if any filters are active
            bool hasActiveFilters = !string.IsNullOrWhiteSpace(query) || typeId.HasValue || countryId.HasValue || 
                regionId.HasValue || grapeId.HasValue || wineryId.HasValue || acidityId.HasValue || 
                minAbv.HasValue || maxAbv.HasValue || minVintage.HasValue || maxVintage.HasValue;
                
            ViewData["HasActiveFilters"] = hasActiveFilters;
            
            // --- Dynamic Wine Type Filter ---
            if (hasActiveFilters)
            {
                // Get wine types that exist in filtered wines
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, wineryId, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "type");
                    
                var validTypeIds = await filteredWines
                    .Select(w => w.TypeId)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["WineTypes"] = await _context.WineTypes
                    .Where(t => validTypeIds.Contains(t.Id))
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            else
            {
                // Show all wine types
                ViewData["WineTypes"] = await _context.WineTypes
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            
            // --- Dynamic Country Filter ---
            if (hasActiveFilters)
            {
                // Get countries that exist in filtered wines
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, wineryId, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "country");
                    
                var validCountryIds = await filteredWines
                    .Select(w => w.CountryId)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["Countries"] = await _context.Countries
                    .Where(c => validCountryIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            else
            {
                // Show all countries
                ViewData["Countries"] = await _context.Countries
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            
            // --- Dynamic Acidity Level Filter ---
            if (hasActiveFilters)
            {
                // Get acidity levels that exist in filtered wines
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, wineryId, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "acidity");
                    
                var validAcidityIds = await filteredWines
                    .Select(w => w.AcidityId)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["AcidityLevels"] = await _context.WineAcidities
                    .Where(a => validAcidityIds.Contains(a.Id))
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }
            else
            {
                // Show all acidity levels
                ViewData["AcidityLevels"] = await _context.WineAcidities
                    .OrderBy(a => a.Name)
                    .ToListAsync();
            }
                
            // --- Dynamic Grape Filter ---
            if (hasActiveFilters)
            {
                // Get grape varieties that exist in filtered wines
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, wineryId, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "grape");
                    
                var validGrapeIds = await filteredWines
                    .SelectMany(w => w.GrapeIds)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["Grapes"] = await _context.Grapes
                    .Where(g => validGrapeIds.Contains(g.Id))
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            }
            else
            {
                // Show all grape varieties
                ViewData["Grapes"] = await _context.Grapes
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            }
            
            // --- Dynamic Region Filter ---
            if (countryId.HasValue)
            {
                // Filter by country first
                var regionQuery = _context.Regions
                    .Where(r => r.CountryId == countryId.Value);
                    
                // If other filters are active, further filter by wines that match those filters
                if (hasActiveFilters && (typeId.HasValue || grapeId.HasValue || wineryId.HasValue || 
                    acidityId.HasValue || minAbv.HasValue || maxAbv.HasValue || 
                    minVintage.HasValue || maxVintage.HasValue || !string.IsNullOrWhiteSpace(query)))
                {
                    // Get wines filtered by everything except region and country
                    var filteredWines = CreateFilteredWineQuery(
                        query, typeId, null, regionId, grapeId, wineryId, 
                        acidityId, minAbv, maxAbv, minVintage, maxVintage, "region");
                        
                    // Get wineries that have matching wines
                    var validWineryIds = await filteredWines
                        .Where(w => w.WineryId.HasValue)
                        .Select(w => w.WineryId.Value)
                        .Distinct()
                        .ToListAsync();
                        
                    // Get regions that have wineries with matching wines
                    var validRegionIds = await _context.Wineries
                        .Where(w => validWineryIds.Contains(w.Id))
                        .Select(w => w.RegionId)
                        .Distinct()
                        .ToListAsync();
                        
                    // Filter regions by both country and valid regions
                    regionQuery = regionQuery.Where(r => validRegionIds.Contains(r.Id));
                }
                
                ViewData["Regions"] = await regionQuery
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            else if (hasActiveFilters)
            {
                // No country selected, but other filters are active
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, wineryId, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "region");
                    
                // Get regions that have wines matching the filters
                var validWineryIds = await filteredWines
                    .Where(w => w.WineryId.HasValue)
                    .Select(w => w.WineryId.Value)
                    .Distinct()
                    .ToListAsync();
                    
                var validRegionIds = await _context.Wineries
                    .Where(w => validWineryIds.Contains(w.Id))
                    .Select(w => w.RegionId)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["Regions"] = await _context.Regions
                    .Where(r => validRegionIds.Contains(r.Id))
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            else
            {
                // No filters active, show all regions
                ViewData["Regions"] = await _context.Regions
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            
            // --- Dynamic Winery Filter ---
            if (regionId.HasValue)
            {
                // Filter by region first
                var wineryQuery = _context.Wineries
                    .Where(w => w.RegionId == regionId.Value);
                    
                // If other filters are active, further filter by wines that match those filters
                if (hasActiveFilters && (typeId.HasValue || countryId.HasValue || grapeId.HasValue || 
                    acidityId.HasValue || minAbv.HasValue || maxAbv.HasValue || 
                    minVintage.HasValue || maxVintage.HasValue || !string.IsNullOrWhiteSpace(query)))
                {
                    // Get wines filtered by everything except winery and region
                    var filteredWines = CreateFilteredWineQuery(
                        query, typeId, countryId, null, grapeId, null, 
                        acidityId, minAbv, maxAbv, minVintage, maxVintage, "winery");
                        
                    // Get wineries that have matching wines
                    var validWineryIds = await filteredWines
                        .Where(w => w.WineryId.HasValue)
                        .Select(w => w.WineryId.Value)
                        .Distinct()
                        .ToListAsync();
                        
                    // Filter wineries by both region and valid wineries
                    wineryQuery = wineryQuery.Where(w => validWineryIds.Contains(w.Id));
                }
                
                ViewData["Wineries"] = await wineryQuery
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            else if (countryId.HasValue)
            {
                // Filter by country but no specific region
                var regionIds = await _context.Regions
                    .Where(r => r.CountryId == countryId.Value)
                    .Select(r => r.Id)
                    .ToListAsync();
                    
                var wineryQuery = _context.Wineries
                    .Where(w => regionIds.Contains(w.RegionId));
                    
                // If other filters are active, further filter by wines that match those filters
                if (hasActiveFilters && (typeId.HasValue || grapeId.HasValue || 
                    acidityId.HasValue || minAbv.HasValue || maxAbv.HasValue || 
                    minVintage.HasValue || maxVintage.HasValue || !string.IsNullOrWhiteSpace(query)))
                {
                    // Get wines filtered by everything except winery
                    var filteredWines = CreateFilteredWineQuery(
                        query, typeId, countryId, null, grapeId, null, 
                        acidityId, minAbv, maxAbv, minVintage, maxVintage, "winery");
                        
                    // Get wineries that have matching wines
                    var validWineryIds = await filteredWines
                        .Where(w => w.WineryId.HasValue)
                        .Select(w => w.WineryId.Value)
                        .Distinct()
                        .ToListAsync();
                        
                    // Filter wineries by both country regions and valid wineries
                    wineryQuery = wineryQuery.Where(w => validWineryIds.Contains(w.Id));
                }
                
                ViewData["Wineries"] = await wineryQuery
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            else if (hasActiveFilters)
            {
                // No region or country selected, but other filters are active
                var filteredWines = CreateFilteredWineQuery(
                    query, typeId, countryId, regionId, grapeId, null, 
                    acidityId, minAbv, maxAbv, minVintage, maxVintage, "winery");
                    
                // Get wineries that have wines matching the filters
                var validWineryIds = await filteredWines
                    .Where(w => w.WineryId.HasValue)
                    .Select(w => w.WineryId.Value)
                    .Distinct()
                    .ToListAsync();
                    
                ViewData["Wineries"] = await _context.Wineries
                    .Where(w => validWineryIds.Contains(w.Id))
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            else
            {
                // No filters active, show all wineries
                ViewData["Wineries"] = await _context.Wineries
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }
            
            // Set selected filters in ViewData
            ViewData["SelectedType"] = typeId;
            ViewData["SelectedCountry"] = countryId;
            ViewData["SelectedRegion"] = regionId;
            ViewData["SelectedGrape"] = grapeId;
            ViewData["SelectedWinery"] = wineryId;
            ViewData["SelectedAcidity"] = acidityId;
            ViewData["MinAbv"] = minAbv;
            ViewData["MaxAbv"] = maxAbv;
            ViewData["MinVintage"] = minVintage;
            ViewData["MaxVintage"] = maxVintage;
            ViewData["SelectedSort"] = sort;
            
            // Always search for wines, with or without filters
            var results = await _searchService.SearchWines(
                query, typeId, countryId, regionId, grapeId, wineryId,  // Re-added wineryId parameter
                acidityId, minAbv, maxAbv, minVintage, maxVintage, sort);
                
            return View(results);
        }

        // Add API endpoint to get regions for a country (for dynamic filtering)
        [HttpGet]
        [Route("api/regions")]
        public async Task<IActionResult> GetRegions(int countryId)
        {
            var regions = await _context.Regions
                .Where(r => r.CountryId == countryId)
                .OrderBy(r => r.Name)
                .Select(r => new { id = r.Id, name = r.Name })
                .ToListAsync();
                
            return Json(regions);
        }
        
        // Add API endpoint to get wineries for a region (for dynamic filtering)
        [HttpGet]
        [Route("api/wineries")]
        public async Task<IActionResult> GetWineries(int regionId)
        {
            var wineries = await _context.Wineries
                .Where(w => w.RegionId == regionId)
                .OrderBy(w => w.Name)
                .Select(w => new { id = w.Id, name = w.Name })
                .ToListAsync();
                
            return Json(wineries);
        }

        // Add endpoint to get wineries for a country
        [HttpGet]
        [Route("api/wineries-by-country")]
        public async Task<IActionResult> GetWineriesByCountry(int countryId)
        {
            var regionIds = await _context.Regions
                .Where(r => r.CountryId == countryId)
                .Select(r => r.Id)
                .ToListAsync();
                
            var wineries = await _context.Wineries
                .Where(w => regionIds.Contains(w.RegionId))
                .OrderBy(w => w.Name)
                .Select(w => new { id = w.Id, name = w.Name, regionId = w.RegionId })
                .ToListAsync();
                
            return Json(wineries);
        }
        
        // API endpoint to get any filter options based on other selected criteria
        [HttpGet]
        [Route("api/filter-options")]
        public async Task<IActionResult> GetFilterOptions(
            string filterType,
            string query = null,
            int? typeId = null, 
            int? countryId = null, 
            int? regionId = null,
            int? grapeId = null,
            int? wineryId = null,
            int? acidityId = null, 
            decimal? minAbv = null, 
            decimal? maxAbv = null,
            int? minVintage = null,
            int? maxVintage = null)
        {
            // Get filtered wine query excluding the requested filter type
            var filteredWines = CreateFilteredWineQuery(
                query, typeId, countryId, regionId, grapeId, wineryId,
                acidityId, minAbv, maxAbv, minVintage, maxVintage, filterType);
                
            // Return different data based on the requested filter type
            switch (filterType.ToLower())
            {
                case "type":
                    var typeIds = await filteredWines
                        .Select(w => w.TypeId)
                        .Distinct()
                        .ToListAsync();
                        
                    var types = await _context.WineTypes
                        .Where(t => typeIds.Contains(t.Id))
                        .OrderBy(t => t.Name)
                        .Select(t => new { id = t.Id, name = t.Name })
                        .ToListAsync();
                        
                    return Json(types);
                    
                case "country":
                    var countryIds = await filteredWines
                        .Select(w => w.CountryId)
                        .Distinct()
                        .ToListAsync();
                        
                    var countries = await _context.Countries
                        .Where(c => countryIds.Contains(c.Id))
                        .OrderBy(c => c.Name)
                        .Select(c => new { id = c.Id, name = c.Name, code = c.Code })
                        .ToListAsync();
                        
                    return Json(countries);
                    
                case "region":
                    // Get wineries from filtered wines that have a winery
                    var wineryIdsForRegion = await filteredWines
                        .Where(w => w.WineryId.HasValue)
                        .Select(w => w.WineryId.Value)
                        .Distinct()
                        .ToListAsync();
                        
                    // Get regions associated with those wineries
                    var regionIds = await _context.Wineries
                        .Where(w => wineryIdsForRegion.Contains(w.Id))
                        .Select(w => w.RegionId)
                        .Distinct()
                        .ToListAsync();
                        
                    // Apply country filter if provided
                    var regionQuery = _context.Regions.AsQueryable();
                    if (countryId.HasValue)
                    {
                        regionQuery = regionQuery.Where(r => r.CountryId == countryId.Value);
                    }
                    
                    var regions = await regionQuery
                        .Where(r => regionIds.Contains(r.Id))
                        .OrderBy(r => r.Name)
                        .Select(r => new { id = r.Id, name = r.Name, countryId = r.CountryId })
                        .ToListAsync();
                        
                    return Json(regions);
                    
                case "winery":
                    // Get valid wineries from filtered wines
                    var validWineryIds = await filteredWines
                        .Where(w => w.WineryId.HasValue)
                        .Select(w => w.WineryId.Value)
                        .Distinct()
                        .ToListAsync();
                        
                    // Apply region filter if provided
                    var wineryQuery = _context.Wineries.AsQueryable();
                    if (regionId.HasValue)
                    {
                        wineryQuery = wineryQuery.Where(w => w.RegionId == regionId.Value);
                    }
                    // Or apply country filter if provided
                    else if (countryId.HasValue)
                    {
                        var countryRegionIds = await _context.Regions
                            .Where(r => r.CountryId == countryId.Value)
                            .Select(r => r.Id)
                            .ToListAsync();
                            
                        wineryQuery = wineryQuery.Where(w => countryRegionIds.Contains(w.RegionId));
                    }
                    
                    var wineries = await wineryQuery
                        .Where(w => validWineryIds.Contains(w.Id))
                        .OrderBy(w => w.Name)
                        .Select(w => new { id = w.Id, name = w.Name, regionId = w.RegionId })
                        .ToListAsync();
                        
                    return Json(wineries);
                    
                case "grape":
                    // Get grape varieties from filtered wines
                    var validGrapeIds = await filteredWines
                        .SelectMany(w => w.GrapeIds)
                        .Distinct()
                        .ToListAsync();
                        
                    var grapes = await _context.Grapes
                        .Where(g => validGrapeIds.Contains(g.Id))
                        .OrderBy(g => g.Name)
                        .Select(g => new { id = g.Id, name = g.Name })
                        .ToListAsync();
                        
                    return Json(grapes);
                    
                case "acidity":
                    var acidityIds = await filteredWines
                        .Select(w => w.AcidityId)
                        .Distinct()
                        .ToListAsync();
                        
                    var acidities = await _context.WineAcidities
                        .Where(a => acidityIds.Contains(a.Id))
                        .OrderBy(a => a.Name)
                        .Select(a => new { id = a.Id, name = a.Name })
                        .ToListAsync();
                        
                    return Json(acidities);
                    
                case "abv-range":
                    // Get min and max ABV values from filtered wines
                    var minPossibleAbv = await filteredWines.MinAsync(w => w.ABV);
                    var maxPossibleAbv = await filteredWines.MaxAsync(w => w.ABV);
                    
                    return Json(new { min = minPossibleAbv, max = maxPossibleAbv });
                    
                case "vintage-range":
                    // This is more complex as vintages are stored as arrays of strings
                    // We'll get all vintages and parse them to integers
                    var allVintageStrings = await filteredWines
                        .SelectMany(w => w.Vintages)
                        .Distinct()
                        .ToListAsync();
                        
                    var allVintages = allVintageStrings
                        .Select(v => int.TryParse(v, out int year) ? year : 0)
                        .Where(y => y > 0)
                        .OrderBy(y => y)
                        .ToList();
                        
                    int minVintageValue = allVintages.Any() ? allVintages.Min() : DateTime.Now.Year - 100;
                    int maxVintageValue = allVintages.Any() ? allVintages.Max() : DateTime.Now.Year;
                    
                    return Json(new { min = minVintageValue, max = maxVintageValue });
                    
                default:
                    return BadRequest($"Unknown filter type: {filterType}");
            }
        }

        [HttpGet]
        [Route("api/search")]
        public async Task<IActionResult> LiveSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<Wine>());
            }
            
            var results = await _searchService.LiveSearch(query);
            
            // Transform to simpler object for JSON response
            var searchResults = results.Select(w => new
            {
                id = w.Id,
                name = w.Name,
                type = w.Type?.Name ?? "Unknown"
            }).ToList();
            
            return Json(searchResults);
        }
    }
}