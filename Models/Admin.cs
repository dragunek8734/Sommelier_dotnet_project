using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnetprojekt.Models
{
    public class Admin
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public string? Role { get; set; }  // Admin,Moderator etc.

        [Required]
        public virtual User User { get; set; } // required relation with user
    }
}
