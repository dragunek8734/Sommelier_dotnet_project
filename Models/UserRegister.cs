using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class UserRegister
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the name")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please enter the password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Please enter the email")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}
