using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class WineType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        // Navigation properties
        public ICollection<Wine> Wines { get; set; }
    }
}
