using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class Region
    {
        public Region()
        {
            // Initialize collections
            Wineries = new List<Winery>();
        }
        
        public int Id { get; set; }
        public string Name { get; set; }
        public int CountryId { get; set; }
        
        // Navigation properties
        public Country Country { get; set; }
        public ICollection<Winery> Wineries { get; set; }
    }
}