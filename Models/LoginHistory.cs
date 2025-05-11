using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class LoginHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string UserAgent { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Device { get; set; } = string.Empty;
        
        public bool Success { get; set; }
        
        [MaxLength(255)]
        public string FailureReason { get; set; } = string.Empty;
        
        // Navigation property
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}