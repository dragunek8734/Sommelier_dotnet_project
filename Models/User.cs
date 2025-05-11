using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CsvHelper.Configuration.Attributes;

namespace dotnetprojekt.Models
{
    public class User
    {
        public User()
        {
            // Initialize collections
            Ratings = new List<Rating>();
            LoginHistory = new List<LoginHistory>();
            CreatedAt = DateTime.UtcNow;
        }
        
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the name")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please enter the password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please enter the email")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage ="Invalid email format")]
        public string Email { get; set; }
        
        // User profile properties
        public string? ProfileImageUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public DateTime? CreatedAt { get; set; }
        
        // Navigation properties
        public ICollection<Rating> Ratings { get; set; }
        
        // Login history
        public ICollection<LoginHistory> LoginHistory { get; set; }


        public int? ReviewCount { get; set; } = 0;


        public int? ExperienceLvlId { get; set; }

        //relation to experience level
        [ForeignKey("ExperienceLvlId")]
        public virtual ExperienceLevel ExperienceLevel { get; set; }
        // relation to admin
        public virtual Admin Admin { get; set; }
    }
}