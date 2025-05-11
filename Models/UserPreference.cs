using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class UserPreference
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        // Wine Type preference (red, white, sparkling, etc.)
        public int? PreferredWineTypeId { get; set; }
        
        // Sweetness preference (1-5 scale: 1=SuperDry, 5=SuperSweet)
        [Range(1, 5)]
        public int? SweetnessPreference { get; set; }
        
        // Body preference (1-5 scale: 1=Light, 5=Full)
        [Range(1, 5)]
        public int? BodyPreference { get; set; }
        
        // Acidity preference
        public int? PreferredAcidityId { get; set; }
        
        // Tannin preference (1-5 scale: 1=Soft, 5=Firm)
        [Range(1, 5)]
        public int? TanninPreference { get; set; }
        
        // Preferred country/region
        public int? PreferredCountryId { get; set; } 
        public int? PreferredRegionId { get; set; }            

        public int[] PrefferedRegions { get; set; } // preffered regions as array of regions IDs

        // Preferred flavor notes as comma-separated text
        public string PreferredFlavors { get; set; }
        
        // Preferred food pairings as array of dish IDs
        public int[] PreferredDishIds { get; set; }

        //added 20:41
        public string Occasion { get; set; }
        

        
        // ABV preference
        public decimal? PreferredAbvMin { get; set; }
        public decimal? PreferredAbvMax { get; set; }
        
        // Last updated timestamp
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [ForeignKey("PreferredWineTypeId")]
        public WineType PreferredWineType { get; set; }
        
        [ForeignKey("PreferredAcidityId")]
        public WineAcidity PreferredAcidity { get; set; }
        
        [ForeignKey("PreferredCountryId")]
        public Country PreferredCountry { get; set; }
        
        [ForeignKey("PreferredRegionId")]
        public Region PreferredRegion { get; set; }

        
    }
}