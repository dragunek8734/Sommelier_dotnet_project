using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace dotnetprojekt.Models
{    public class Wine
    {
        public Wine()
        {
            // Initialize collections
            Ratings = new List<Rating>();
        }
        
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TypeId { get; set; }
        public string Elaborate { get; set; } = string.Empty;
        
        // Store as JSON in PostgreSQL
        public int[] GrapeIds { get; set; } = Array.Empty<int>();
        public int[] PairWithIds { get; set; } = Array.Empty<int>();
        
        // Store ABV as decimal percentage (e.g., 12.5 for 12.5%)
        public decimal ABV { get; set; }
        public int AcidityId { get; set; }
        public int CountryId { get; set; }
        
        // Store as JSON in PostgreSQL
        public string[] Vintages { get; set; } = Array.Empty<string>();
        
        // Navigation properties
        public WineType? Type { get; set; }
        public WineAcidity? Acidity { get; set; }
        public Country? Country { get; set; }        public ICollection<Rating> Ratings { get; set; }
        
        // One-to-many: A wine belongs to one winery
        public int? WineryId { get; set; }
        public Winery? Winery { get; set; }
        
        // Many-to-many navigation properties
        [NotMapped]
        public ICollection<Grape>? Grapes { get; set; }
        
        [NotMapped]
        public ICollection<Dish>? PairedDishes { get; set; }

        [NotMapped]
        public NpgsqlTsVector? SearchVector { get; set; }
    }
}