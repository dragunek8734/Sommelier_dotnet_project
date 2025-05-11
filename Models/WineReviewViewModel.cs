using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace dotnetprojekt.Models
{
    public class WineReviewViewModel
    {
        public int WineId { get; set; }

        [Required]
        [Range(0.5, 5.0, ErrorMessage = "Please rate the wine from 0.5 to 5 stars")]
        public decimal Rating { get; set; }

        [Required(ErrorMessage = "Please provide a description of your experience with this wine")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        // Optional image upload
        public IFormFile? ReviewImage { get; set; }

        // Maximum file size (5MB)
        public const int MaxFileSize = 5 * 1024 * 1024;

        // Allowed file extensions
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    }
}
