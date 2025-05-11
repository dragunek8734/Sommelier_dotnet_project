using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class ExperienceLevel
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }
        // user collection
        public virtual ICollection<User> Users { get; set; }
    }
}
