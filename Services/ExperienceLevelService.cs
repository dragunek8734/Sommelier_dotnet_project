using System.Net.WebSockets;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnetprojekt.Services
{
    public interface IExperienceLevelService
    {
        Task UpdateExperienceLevelAsync(int userId);
    }
    public class ExperienceLevelService : IExperienceLevelService
    {
        
        private readonly WineLoversContext _context;
        private readonly ILogger<ExperienceLevelService> _logger;

        public ExperienceLevelService(WineLoversContext context, ILogger<ExperienceLevelService> logger)
        {
            _context = context;
            _logger = logger;
        }


        public async Task UpdateExperienceLevelAsync(int userId)
        {
            try
            {
                // getting real user reviewcount
                var reviewCount = await _context.Ratings.CountAsync(r => r.UserId == userId);

                // Determine the experience level ID based on review count
                int explv = GetExperienceLevelIdByReviewCount(reviewCount);

                // getting valid experience level
                var experiencelevel = await _context.ExperienceLevels
                                                     .FirstOrDefaultAsync(e => e.Id == explv);

                if (experiencelevel == null)
                {
                    throw new InvalidOperationException("Experience level does not exist");
                }

                // getting user based on userId
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User does not exist.");
                }

                // updating user experience level
                user.ExperienceLvlId = experiencelevel.Id;
                user.ReviewCount = reviewCount;

                // Saving changes to database
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log exception
                _logger.LogError(ex, "Error occurred while updating the experience level for user: {UserId}", userId);
                throw new InvalidOperationException("Error during user experience level update.", ex);
            }
        }

        private int GetExperienceLevelIdByReviewCount(int reviewCount)
        {
            // Determine the experience level ID based on review count
            return reviewCount switch
            {
                < 11 => 1,  // 0–10 reviews begginer
                < 51 => 2,  // 11–50 reviews enthusiast
                < 151 => 3, // 51–150 reviews  amateur-exper    
                < 501 => 4, // 151–500 reviews  sommelier
                _ => 5      // 501+ reviews     master of taste
            };
        }


    }
}
