using System.Collections.Generic;

namespace dotnetprojekt.Models
{
    public class QuizPreferences
    {
        public int? PreferredWineTypeId { get; set; }
        public int? PreferredAcidityId { get; set; }
        public int? PreferredRegionId { get; set; }
        public int? PreferredCountryId { get; set; }
        public List<int> PreferredDishIds { get; set; }
        public List<string> PreferredFlavors { get; set; }
        public List<int> PreferredRegionIds { get; set; } // added
        public int BodyPreference { get; set; }

        // Added 20:42
        public string Occasion { get; set; }
    }
}