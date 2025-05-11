using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace dotnetprojekt.Models
{
    // This class represents a view/materialized view in the database
    // It will be configured via Fluent API in the DbContext
    public class TopRatedWinesView
    {
        public int WineId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        
        // Navigation property to the referenced Wine
        public virtual Wine Wine { get; set; }
    }
}