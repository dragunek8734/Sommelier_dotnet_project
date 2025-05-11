using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class Grape
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        // Navigation properties
        [NotMapped]
        public ICollection<Wine> Wines { get; set; }
    }
}
