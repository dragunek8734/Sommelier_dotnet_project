using System;
using System.Collections.Generic;

namespace dotnetprojekt.Models
{
    // DTO class specifically for search results to use with SqlQueryRaw
    public class WineSearchResult
    {
        // Wine properties
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Elaborate { get; set; } = string.Empty;
        public decimal? ABV { get; set; }
        public int[] GrapeIds { get; set; } = Array.Empty<int>();
        public string[] Vintages { get; set; } = Array.Empty<string>();
        public int[] PairWithIds { get; set; } = Array.Empty<int>();
        
        // Related entity IDs
        public int TypeId { get; set; }
        public int CountryId { get; set; }
        public int AcidityId { get; set; }
        
        // Extra properties from joins for display
        public string TypeName { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string AcidityName { get; set; } = string.Empty;
        
        // Helper method to convert to Wine entity
        public Wine ToWineEntity()
        {
            return new Wine
            {
                Id = Id,
                Name = Name,
                Elaborate = Elaborate,
                // Remove Winery as it doesn't exist in Wine class
                // Convert nullable ABV to non-nullable with default value if null
                ABV = ABV ?? 0m,
                // Remove Description and Image as they don't exist in Wine class
                GrapeIds = GrapeIds,
                Vintages = Vintages,
                PairWithIds = PairWithIds,
                TypeId = TypeId,
                CountryId = CountryId,
                AcidityId = AcidityId,
                
                // Initialize navigation properties
                Type = new WineType { Id = TypeId, Name = TypeName },
                Country = new Country { Id = CountryId, Name = CountryName, Code = CountryCode },
                Acidity = new WineAcidity { Id = AcidityId, Name = AcidityName }
            };
        }
    }
}