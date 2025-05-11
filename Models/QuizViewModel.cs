using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace dotnetprojekt.Models
{
    public class QuizViewModel
    {
        // Wine type preference
        [Display(Name = "What type of wine do you prefer?")]
        public string WineTypePreference { get; set; }
        
        // Sweetness preference on a scale of 1-5
        [Display(Name = "How sweet do you like your wine?")]
        [Range(1, 5, ErrorMessage = "Please select a value between 1 and 5")]
        public int SweetnessPreference { get; set; }
        
        // Acidity preference (matches WineAcidity.Id)
        [Display(Name = "What level of acidity do you prefer?")]
        public int? AcidityPreference { get; set; }
        
        // Body preference on a scale of 1-5 (1 = light, 5 = full)
        [Display(Name = "How full-bodied do you like your wine?")]
        [Range(1, 5, ErrorMessage = "Please select a value between 1 and 5")]
        public int BodyPreference { get; set; }
        
        // Flavor profiles preference
        [Display(Name = "What flavor profile do you prefer?")]
        public string FlavorPreference { get; set; } // Options: fruity, earthy, spicy
        
        // Food pairings
        [Display(Name = "What foods do you typically eat with wine?")]
        public List<string> FoodPairings { get; set; }
        

        // Occasion
        [Display(Name = "What occasions do you usually drink wine for?")]
        public string Occasion { get; set; }
    }
}