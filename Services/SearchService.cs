using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Services
{
    public class SearchService
    {
        private readonly WineLoversContext _context;
        private readonly ILogger<SearchService> _logger;
        private const int DEFAULT_SEARCH_LIMIT = 50;

        // Define sorting options as enum
        public enum SortOption
        {
            Relevance,      // Default for text search
            NameAsc,        // A to Z
            NameDesc,       // Z to A
            RatingDesc,     // Highest rated first
            RatingAsc,      // Lowest rated first
            PriceAsc,       // Lowest price first
            PriceDesc       // Highest price first
        }

        public SearchService(WineLoversContext context, ILogger<SearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Wine>> SearchWines(
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
            SortOption sortBy = SortOption.Relevance,
            int limit = DEFAULT_SEARCH_LIMIT)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var stepStopwatch = Stopwatch.StartNew();
            List<Wine> wines;

            // Build the filter conditions
            List<string> filterConditions = new List<string>();
            List<string> filterDebug = new List<string>();

            if (typeId.HasValue)
            {
                filterConditions.Add($"w.\"TypeId\" = {typeId.Value}");
                filterDebug.Add($"typeId={typeId.Value}");
            }

            if (countryId.HasValue)
            {
                filterConditions.Add($"w.\"CountryId\" = {countryId.Value}");
                filterDebug.Add($"countryId={countryId.Value}");
            }            // Add region filter back
            if (regionId.HasValue)
            {
                // Get all winery IDs from this region
                var wineryIds = await _context.Wineries
                    .Where(w => w.RegionId == regionId.Value)
                    .Select(w => w.Id)
                    .ToListAsync();
                
                if (wineryIds.Any())
                {
                    // Filter wines by WineryId directly using a one-to-many relationship
                    filterConditions.Add($"w.\"WineryId\" IN ({string.Join(",", wineryIds)})");
                    filterDebug.Add($"regionId={regionId.Value} (wineries: {wineryIds.Count})");
                }
                else
                {
                    _logger.LogWarning($"No wineries found for region {regionId.Value}");
                    return new List<Wine>(); // Return empty if no wineries in region
                }
            }

            if (wineryId.HasValue)
            {
                filterConditions.Add($"w.\"WineryId\" = {wineryId.Value}");
                filterDebug.Add($"wineryId={wineryId.Value}");
            }

            if (acidityId.HasValue)
            {
                filterConditions.Add($"w.\"AcidityId\" = {acidityId.Value}");
                filterDebug.Add($"acidityId={acidityId.Value}");
            }

            if (minAbv.HasValue)
            {
                filterConditions.Add($"w.\"ABV\" >= {minAbv.Value}");
                filterDebug.Add($"minAbv={minAbv.Value}");
            }

            if (maxAbv.HasValue)
            {
                filterConditions.Add($"w.\"ABV\" <= {maxAbv.Value}");
                filterDebug.Add($"maxAbv={maxAbv.Value}");
            }

            if (grapeId.HasValue)
            {
                // Use PostgreSQL array contains operator for arrays stored as JSON
                filterConditions.Add($"w.\"GrapeIds\" @> ARRAY[{grapeId.Value}]::integer[]");
                filterDebug.Add($"grapeId={grapeId.Value}");
            }

            // Handle vintage range filtering
            if (minVintage.HasValue)
            {
                // This assumes vintages are stored as text values that can be cast to integers
                filterConditions.Add($"EXISTS (SELECT 1 FROM unnest(w.\"Vintages\") AS v WHERE CAST(v AS integer) >= {minVintage.Value})");
                filterDebug.Add($"minVintage={minVintage.Value}");
            }

            if (maxVintage.HasValue)
            {
                // This assumes vintages are stored as text values that can be cast to integers
                filterConditions.Add($"EXISTS (SELECT 1 FROM unnest(w.\"Vintages\") AS v WHERE CAST(v AS integer) <= {maxVintage.Value})");
                filterDebug.Add($"maxVintage={maxVintage.Value}");
            }

            string filtersLog = filterDebug.Any() ? string.Join(", ", filterDebug) : "none";
            _logger.LogInformation($"Applied filters: {filtersLog}");

            // Text search with additional filters
            if (!string.IsNullOrWhiteSpace(query))
            {
                string sanitizedQuery = query.Replace("'", "''");
                _logger.LogInformation($"Starting text search for query: '{query}' with filters: {filtersLog}");

                // Split the query into words for partial matching
                string[] searchWords = sanitizedQuery.Split(new char[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Build conditions for partial word matching
                List<string> wordConditions = new List<string>();
                foreach (var word in searchWords)
                {
                    // Use ILIKE with wildcards for partial word matching
                    wordConditions.Add($"w.\"Name\" ILIKE '%{word}%'");
                    
                    // Also add conditions for the elaborate field with lower priority
                    wordConditions.Add($"w.\"Elaborate\" ILIKE '%{word}%'");
                }
                
                // Combine word conditions with OR (match any word in name or description)
                string partialMatchClause = string.Join(" OR ", wordConditions);
                
                // Keep similarity search as a fallback for fuzzy matching
                string similarityClause = $"(similarity(w.\"Name\", '{sanitizedQuery}') > 0.3 OR similarity(w.\"Elaborate\", '{sanitizedQuery}') > 0.2)";
                
                // Build WHERE clause that combines text search with other filters
                string textSearchClause = $"({partialMatchClause}) OR {similarityClause}";
                
                // Combine text search with other filters
                string whereClause;
                if (filterConditions.Any())
                {
                    // Ensure text search AND other filters are applied together
                    // This means results must match both text search AND all filters
                    whereClause = $"({textSearchClause}) AND ({string.Join(" AND ", filterConditions)})";
                }
                else
                {
                    whereClause = textSearchClause;
                }

                // Define ORDER BY clause based on sortBy parameter
                string orderBy;
                switch (sortBy)
                {
                    case SortOption.NameAsc:
                        orderBy = "w.\"Name\" ASC";
                        break;
                    case SortOption.NameDesc:
                        orderBy = "w.\"Name\" DESC";
                        break;
                    case SortOption.RatingDesc:
                        orderBy = "(SELECT AVG(r.\"RatingValue\") FROM \"Ratings\" r WHERE r.\"WineId\" = w.\"Id\") DESC NULLS LAST";
                        break;
                    case SortOption.RatingAsc:
                        orderBy = "(SELECT AVG(r.\"RatingValue\") FROM \"Ratings\" r WHERE r.\"WineId\" = w.\"Id\") ASC NULLS LAST";
                        break;
                    case SortOption.PriceAsc:
                        orderBy = "w.\"Name\" ASC"; // Changed from w."RetailPrice" to w."Name" as fallback
                        break;
                    case SortOption.PriceDesc:
                        orderBy = "w.\"Name\" DESC"; // Changed from w."RetailPrice" to w."Name" as fallback
                        break;
                    case SortOption.Relevance:
                    default:
                        // Calculate custom relevance score prioritizing exact partial matches
                        orderBy = $@"CASE 
                                WHEN ({partialMatchClause}) THEN 1.0 
                                ELSE similarity(w.""Name"", '{sanitizedQuery}')
                            END DESC";
                        break;
                }

                // Using more efficient SQL for text search with filters
                // Using WineSearchResult DTO instead of Wine entity
                var sql = $@"
                    WITH filtered_wines AS (
                        SELECT w.*,
                            CASE 
                                WHEN ({partialMatchClause}) THEN 1.0 
                                ELSE similarity(w.""Name"", '{sanitizedQuery}')
                            END as match_score
                        FROM ""Wines"" w
                        WHERE {whereClause}
                        ORDER BY match_score DESC, {(sortBy == SortOption.Relevance ? "w.\"Name\" ASC" : orderBy)}
                        LIMIT {limit}
                    )
                    SELECT w.*, t.""Name"" as TypeName, 
                           c.""Name"" as CountryName, c.""Code"" as CountryCode,
                           a.""Name"" as AcidityName
                    FROM filtered_wines w
                    LEFT JOIN ""Wine_types"" t ON w.""TypeId"" = t.""Id""
                    LEFT JOIN ""Country"" c ON w.""CountryId"" = c.""Id""
                    LEFT JOIN ""Wine_acidity"" a ON w.""AcidityId"" = a.""Id""
                    ORDER BY w.match_score DESC, {(sortBy == SortOption.Relevance ? "w.\"Name\" ASC" : orderBy)}";

                stepStopwatch.Restart();
                var searchResults = await _context.Database.SqlQueryRaw<WineSearchResult>(sql).ToListAsync();
                _logger.LogInformation($"Text search query execution took: {stepStopwatch.ElapsedMilliseconds}ms, found {searchResults.Count} results");
                
                // Convert WineSearchResult objects to Wine entities
                wines = searchResults.Select(result => result.ToWineEntity()).ToList();
            }
            else
            {
                _logger.LogInformation($"Starting filter-based search with filters: {filtersLog}");
                
                // Use a more efficient approach for filter-based searches
                var query1 = _context.Wines.AsNoTracking(); // Use AsNoTracking for read-only queries
                
                stepStopwatch.Restart();
                
                // Apply filters with optimized query flow
                if (typeId.HasValue)
                {
                    query1 = query1.Where(w => w.TypeId == typeId.Value);
                }

                if (countryId.HasValue)
                {
                    query1 = query1.Where(w => w.CountryId == countryId.Value);
                }

                if (acidityId.HasValue)
                {
                    query1 = query1.Where(w => w.AcidityId == acidityId.Value);
                }

                if (minAbv.HasValue)
                {
                    query1 = query1.Where(w => w.ABV >= minAbv.Value);
                }

                if (maxAbv.HasValue)
                {
                    query1 = query1.Where(w => w.ABV <= maxAbv.Value);
                }
                
                // Apply grape filter if specified
                if (grapeId.HasValue)
                {
                    // Use PostgreSQL array contains function
                    query1 = query1.Where(w => w.GrapeIds.Contains(grapeId.Value));
                }
                  // Add region filter back
                if (regionId.HasValue)
                {
                    // Get all winery IDs from this region
                    var wineryIds = await _context.Wineries
                        .Where(w => w.RegionId == regionId.Value)
                        .Select(w => w.Id)
                        .ToListAsync();
                    
                    if (wineryIds.Any())
                    {
                        // Filter wines by WineryId directly since we have a one-to-many relationship now
                        query1 = query1.Where(w => wineryIds.Contains(w.WineryId ?? 0));
                        
                        _logger.LogInformation($"Applied region filter: regionId={regionId.Value}, found {wineryIds.Count} wineries");
                    }
                    else
                    {
                        _logger.LogWarning($"No wineries found for region {regionId.Value}");
                        return new List<Wine>(); // Return empty if no wineries in region
                    }
                }
                
                // Add winery filter back
                if (wineryId.HasValue)
                {
                    // Direct filtering on WineryId instead of using the join table
                    query1 = query1.Where(w => w.WineryId == wineryId.Value);
                    _logger.LogInformation($"Applied winery filter: wineryId={wineryId.Value}");
                }
                
                // Handle vintage filtering
                // Note: We're not doing this in LINQ because Entity Framework can't translate int.TryParse
                // to SQL, so we'll need to execute the query first and then filter in memory.
                var hasVintageFilter = minVintage.HasValue || maxVintage.HasValue;

                _logger.LogInformation($"Filter application took: {stepStopwatch.ElapsedMilliseconds}ms");

                // Apply sorting based on the sortBy parameter
                IOrderedQueryable<Wine> orderedQuery;
                switch (sortBy)
                {
                    case SortOption.NameAsc:
                        orderedQuery = query1.OrderBy(w => w.Name);
                        break;
                    case SortOption.NameDesc:
                        orderedQuery = query1.OrderByDescending(w => w.Name);
                        break;
                    case SortOption.RatingDesc:
                        orderedQuery = query1.OrderByDescending(w => w.Ratings.Average(r => r.RatingValue));
                        break;
                    case SortOption.RatingAsc:
                        orderedQuery = query1.OrderBy(w => w.Ratings.Average(r => r.RatingValue));
                        break;
                    case SortOption.PriceAsc:
                        // Changed: orderedQuery = query1.OrderBy(w => w.RetailPrice);
                        // Since Wine doesn't have RetailPrice property, we'll order by name as a fallback
                        orderedQuery = query1.OrderBy(w => w.Name);
                        break;
                    case SortOption.PriceDesc:
                        // Changed: orderedQuery = query1.OrderByDescending(w => w.RetailPrice);
                        // Since Wine doesn't have RetailPrice property, we'll order by name as a fallback
                        orderedQuery = query1.OrderByDescending(w => w.Name);
                        break;
                    case SortOption.Relevance:
                    default:
                        orderedQuery = query1.OrderBy(w => w.Name); // Default to name sorting when no text query
                        break;
                }

                // If we have vintage filtering, we need to execute the query and then filter in memory
                if (hasVintageFilter)
                {
                    stepStopwatch.Restart();
                    var allWines = await orderedQuery.ToListAsync();
                    _logger.LogInformation($"Initial query execution took: {stepStopwatch.ElapsedMilliseconds}ms, found {allWines.Count} results");
                    
                    // Apply vintage filters in memory
                    if (minVintage.HasValue)
                    {
                        allWines = allWines.Where(w => w.Vintages.Any(v => 
                            int.TryParse(v, out int vintage) && vintage >= minVintage.Value)).ToList();
                        _logger.LogInformation($"After minVintage filter: {allWines.Count} results");
                    }
                    
                    if (maxVintage.HasValue)
                    {
                        allWines = allWines.Where(w => w.Vintages.Any(v => 
                            int.TryParse(v, out int vintage) && vintage <= maxVintage.Value)).ToList();
                        _logger.LogInformation($"After maxVintage filter: {allWines.Count} results");
                    }
                    
                    // Take only the requested limit
                    wines = allWines.Take(limit).ToList();
                    
                    // Load related entities and return
                    await LoadRelatedEntities(wines);
                    totalStopwatch.Stop();
                    _logger.LogInformation($"Total search process took: {totalStopwatch.ElapsedMilliseconds}ms");
                    return wines;
                }
                else
                {
                    // Standard flow without vintage filtering
                    // Use optimized selection with projection to reduce data transfer
                    var wineQuery = orderedQuery
                        .Select(w => new
                        {
                            Wine = w,
                            Type = w.Type,
                            Country = w.Country,
                            Acidity = w.Acidity
                        })
                        .Take(limit);

                    stepStopwatch.Restart();
                    var results = await wineQuery.ToListAsync();
                    wines = results.Select(r => {
                        var wine = r.Wine;
                        wine.Type = r.Type;
                        wine.Country = r.Country;
                        wine.Acidity = r.Acidity;
                        return wine;
                    }).ToList();
                    
                    _logger.LogInformation($"Filter query execution took: {stepStopwatch.ElapsedMilliseconds}ms, found {wines.Count} results");
                }
            }
            
            // Load related entities for the wine list
            stepStopwatch.Restart();
            await LoadRelatedEntities(wines);
            _logger.LogInformation($"Loading related entities took: {stepStopwatch.ElapsedMilliseconds}ms");
            
            totalStopwatch.Stop();
            _logger.LogInformation($"Total search process took: {totalStopwatch.ElapsedMilliseconds}ms");
            
            return wines;
        }

        // Keep the optimized method for loading related entities
        private async Task LoadRelatedEntities(List<Wine> wines)
        {
            if (wines.Count == 0)
                return;
                
            // Get all unique grape IDs across all wines
            var allGrapeIds = wines
                .SelectMany(w => w.GrapeIds)
                .Distinct()
                .ToArray();
                
            // Get all unique dish IDs across all wines
            var allDishIds = wines
                .SelectMany(w => w.PairWithIds)
                .Distinct()
                .ToArray();
            
            // Fetch all needed grapes in a single query
            Dictionary<int, Grape> grapesById = new Dictionary<int, Grape>();
            if (allGrapeIds.Length > 0)
            {
                var grapes = await _context.Grapes
                    .Where(g => allGrapeIds.Contains(g.Id))
                    .ToListAsync();
                    
                grapesById = grapes.ToDictionary(g => g.Id);
            }
            
            // Fetch all needed dishes in a single query
            Dictionary<int, Dish> dishesById = new Dictionary<int, Dish>();
            if (allDishIds.Length > 0)
            {
                var dishes = await _context.Dishes
                    .Where(d => allDishIds.Contains(d.Id))
                    .ToListAsync();
                    
                dishesById = dishes.ToDictionary(d => d.Id);
            }
            
            // Assign related entities to each wine from our cached collections
            foreach (var wine in wines)
            {
                // Assign grapes
                if (wine.GrapeIds.Length > 0)
                {
                    wine.Grapes = wine.GrapeIds
                        .Where(id => grapesById.ContainsKey(id))
                        .Select(id => grapesById[id])
                        .ToList();
                }
                else
                {
                    wine.Grapes = new List<Grape>();
                }
                
                // Assign paired dishes
                if (wine.PairWithIds.Length > 0)
                {
                    wine.PairedDishes = wine.PairWithIds
                        .Where(id => dishesById.ContainsKey(id))
                        .Select(id => dishesById[id])
                        .ToList();
                }
                else
                {
                    wine.PairedDishes = new List<Dish>();
                }
            }
        }

        public async Task<List<Wine>> LiveSearch(string query, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Wine>();
            }

            string sanitizedQuery = query.Replace("'", "''");
            
            // Step 1: Create a word array by splitting the query
            string[] searchWords = sanitizedQuery.Split(new char[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Step 2: Build a dynamic WHERE clause for partial word matching
            List<string> wordConditions = new List<string>();
            
            // Add conditions for each word to match partially
            foreach (var word in searchWords)
            {
                // Use ILIKE with wildcards to enable partial word matching
                // This searches for the word anywhere within any word in the name
                wordConditions.Add($"w.\"Name\" ILIKE '%{word}%'");
                
                // Also consider using the word for similarity matching as a fallback
                // This helps capture cases where the partial match might be too broad
            }
            
            // Combine word conditions with OR (match any word)
            string partialMatchClause = string.Join(" OR ", wordConditions);
            
            // Step 3: Also include similarity for fuzzy matching as a parallel condition
            string similarityClause = $"similarity(w.\"Name\", '{sanitizedQuery}') > 0.3";
            
            // Final WHERE clause combines partial word matching OR similarity
            string whereClause = $"({partialMatchClause}) OR ({similarityClause})";
            
            // More efficient SQL for live search with partial word matching
            // Using WineSearchResult DTO instead of Wine entity
            var sql = $@"
                SELECT w.*, 
                       t.""Name"" as TypeName,
                       c.""Name"" as CountryName,
                       c.""Code"" as CountryCode,
                       a.""Name"" as AcidityName,
                       CASE 
                          WHEN ({partialMatchClause}) THEN 1.0 
                          ELSE similarity(w.""Name"", '{sanitizedQuery}')
                       END as match_score
                FROM ""Wines"" w
                LEFT JOIN ""Wine_types"" t ON w.""TypeId"" = t.""Id""
                LEFT JOIN ""Country"" c ON w.""CountryId"" = c.""Id""
                LEFT JOIN ""Wine_acidity"" a ON w.""AcidityId"" = a.""Id""
                WHERE {whereClause}
                ORDER BY match_score DESC, w.""Name"" ASC
                LIMIT {limit}";

            _logger.LogInformation($"Executing partial word search for: '{query}'");
            var searchResults = await _context.Database.SqlQueryRaw<WineSearchResult>(sql).ToListAsync();
            
            // Convert WineSearchResult objects to Wine entities
            return searchResults.Select(result => result.ToWineEntity()).ToList();
        }
    }
}
