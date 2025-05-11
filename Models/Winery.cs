using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{    public class Winery
    {
        public Winery()
        {
            // Initialize collections
            Wines = new List<Wine>();
        }
        
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Website { get; set; }
        public int RegionId { get; set; }
        
        // Navigation properties
        public Region? Region { get; set; }
        
        // One-to-many: A winery can have many wines
        public ICollection<Wine> Wines { get; set; }
    }
}