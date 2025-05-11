using System;
using System.Collections.Generic;

namespace dotnetprojekt.Models
{
    public class RatingViewModel
    {
        public int Id { get; set; }
        public int WineId { get; set; }
        public string WineName { get; set; } = string.Empty;
        public decimal RatingValue { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class LoginHistoryViewModel
    {
        public DateTime Date { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
    }

    public class UserPreferencesViewModel
    {
        public string FavoriteWineType { get; set; } = string.Empty;
        public string FavoriteRegion { get; set; } = string.Empty;
        public string FavoriteCountry { get; set; } = string.Empty;
        public int? Sweetness { get; set; }
        public int? Body { get; set; }
        public string Acidity { get; set; } = string.Empty;
        public int? Tannin { get; set; }
        public decimal? AbvMin { get; set; }
        public decimal? AbvMax { get; set; }
        public string Flavors { get; set; } = string.Empty;
        public int[] FoodPairings { get; set; } = Array.Empty<int>();
        public DateTime LastUpdated { get; set; }
        
        // Display properties
        public string Experience { get; set; } = string.Empty;
        public List<string> TypicalOccasions { get; set; } = new List<string>();
        public List<string> FavoriteWineTypes { get; set; } = new List<string>();
        public List<string> FavoriteRegions { get; set; } = new List<string>();
        public List<string> FavoriteFlavors 
        { 
            get 
            {
                return !string.IsNullOrEmpty(Flavors) 
                    ? new List<string>(Flavors.Split(',', StringSplitOptions.RemoveEmptyEntries)) 
                    : new List<string>();
            } 
        }
        public List<string> FavoriteDishes { get; set; } = new List<string>();
    }

    public class NotificationSettingsViewModel
    {
        public bool Email { get; set; }
        public bool Push { get; set; }
        public bool Sms { get; set; }
    }

    public class PrivacySettingsViewModel
    {
        public bool ProfileVisible { get; set; }
        public bool TastingHistoryVisible { get; set; }
    }

    public class UserAccountViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; } = DateTime.UtcNow.AddMonths(-1);
        public IEnumerable<RatingViewModel> Ratings { get; set; } = new List<RatingViewModel>();
        public UserPreferencesViewModel Preferences { get; set; } = new UserPreferencesViewModel();
        public NotificationSettingsViewModel NotificationSettings { get; set; } = new NotificationSettingsViewModel();
        public PrivacySettingsViewModel PrivacySettings { get; set; } = new PrivacySettingsViewModel();
        public IEnumerable<LoginHistoryViewModel> LoginHistory { get; set; } = new List<LoginHistoryViewModel>();
        
        // Unified settings for the view
        public bool EmailNotifications => NotificationSettings.Email;
        public bool PushNotifications => NotificationSettings.Push;
        public bool SmsNotifications => NotificationSettings.Sms;
        public bool ProfileVisible => PrivacySettings.ProfileVisible;
        public bool TastingHistoryVisible => PrivacySettings.TastingHistoryVisible;
        public bool TwoFactorEnabled { get; set; } = false;
    }
}