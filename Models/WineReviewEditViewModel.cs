namespace dotnetprojekt.Models
{
    public class WineReviewEditViewModel
    {
        public int Id { get; set; }
        public int WineId { get; set; }
        public string WineName { get; set; } = string.Empty;
        public decimal RatingValue { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public IFormFile? ReviewImage { get; set; }
        // Maximum file size (5MB)
        public const int MaxFileSize = 5 * 1024 * 1024;

        // Allowed file extensions
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    }
}

