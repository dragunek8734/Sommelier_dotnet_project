using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace dotnetprojekt.Models
{
    public class UserAccountEditViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        // Current profile image url to display
        public string? ProfileImageUrl { get; set; }

        // File upload for profile image
        public IFormFile? ProfileImage { get; set; }

        // Maximum file size (5MB)
        public const int MaxFileSize = 5 * 1024 * 1024;

        // Allowed file extensions
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    }
}