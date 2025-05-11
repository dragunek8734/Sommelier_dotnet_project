using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int WineId { get; set; }
        
        [Column(TypeName = "decimal(3,1)")]
        [Range(1, 5)]
        public decimal RatingValue { get; set; }
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string ImagePath { get; set; } = string.Empty;
        
        public DateTime Date { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public Wine Wine { get; set; }
    }
}