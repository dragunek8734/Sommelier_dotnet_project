using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;  // Add this line

namespace dotnetprojekt.Models
{
    public class Dish
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        // Navigation properties
        [NotMapped]
        public ICollection<Wine> PairedWithWines { get; set; }
    }
}