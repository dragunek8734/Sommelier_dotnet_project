using System;
using System.Collections.Generic;

namespace dotnetprojekt.Models
{
    public class PersonalRecommendationsViewModel
    {
        public UserPreference Preferences { get; set; }
        public List<Wine> Recommendations { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}