using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using dotnetprojekt.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace dotnetprojekt.Controllers
{
    public class BlogController : Controller
    {
        private readonly ILogger<BlogController> _logger;
        private readonly WineLoversContext _context;

        public BlogController(ILogger<BlogController> logger, WineLoversContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Get blog posts (in a real app, these would come from the database)
            var posts = GetSampleBlogPosts();
            return View(posts);
        }

        public IActionResult Post(int id)
        {
            // In a real app, we would fetch the post from database
            var posts = GetSampleBlogPosts();
            var post = posts.FirstOrDefault(p => p.Id == id);
            
            if (post == null)
            {
                return NotFound();
            }
            
            return View(post);
        }

        private List<BlogPost> GetSampleBlogPosts()
        {
            // Sample blog posts about wine education
            return new List<BlogPost>
            {
                new BlogPost
                {
                    Id = 1,
                    Title = "Understanding Wine Acidity: The Backbone of Great Wines",
                    Excerpt = "Discover how acidity affects the taste and balance of wines, and why it's considered the backbone of great wines around the world.",
                    Content = "<p>Wine acidity is often described as the backbone of a wine, providing structure, freshness, and balance. Without proper acidity, wines can taste flabby, dull, and uninteresting.</p><p>Acidity in wine comes primarily from tartaric acid, malic acid, and citric acid. Each of these acids contributes differently to the overall taste profile:</p><ul><li><strong>Tartaric Acid</strong>: The primary acid in grapes and the most important in winemaking. It provides that sharp, tart sensation on the sides of your tongue.</li><li><strong>Malic Acid</strong>: Found in many fruits, especially green apples. It gives a crisp, green apple-like sharpness to wines.</li><li><strong>Citric Acid</strong>: Present in smaller amounts, it contributes citrus notes and freshness.</li></ul><p>Climate plays a crucial role in determining a wine's acidity. Grapes grown in cooler regions like Germany, northern France, and New Zealand typically retain more acidity than those from warmer regions like Australia or California.</p><p>When tasting wine, you can assess acidity by how much your mouth waters after taking a sip. High-acid wines like Riesling or Sauvignon Blanc will stimulate saliva production, while low-acid wines like some Chardonnays or Viogniers will have less of this effect.</p><p>Understanding acidity helps with food pairing. High-acid wines pair wonderfully with fatty or rich foods because the acidity cuts through the richness, cleansing the palate between bites.</p>",
                    ImageUrl = "/images/blog/wine-acidity.jpg",
                    PublishedDate = DateTime.Now.AddDays(-5),
                    Author = "Emma Vintner",
                    Tags = new List<string> { "Wine Basics", "Tasting Notes", "Wine Chemistry" }
                },
                new BlogPost
                {
                    Id = 2,
                    Title = "The Art of Wine and Cheese Pairing",
                    Excerpt = "Learn the principles behind perfect wine and cheese pairings and discover combinations that will elevate your next gathering.",
                    Content = "<p>Wine and cheese have been natural companions for centuries, with the right pairing elevating both to new heights of flavor. While there are no strict rules, understanding a few basic principles can help create memorable pairings.</p><p><strong>Match intensity levels</strong>: Delicate cheeses pair best with lighter wines, while robust cheeses need fuller-bodied wines. A subtle goat cheese would be overwhelmed by a bold Cabernet Sauvignon but sings alongside a crisp Sauvignon Blanc.</p><p><strong>Consider regionality</strong>: Often, wines and cheeses from the same region naturally complement each other, having evolved together over generations. Think of Spanish Manchego with Tempranillo or French Brie with Champagne.</p><p><strong>Balance textures</strong>: Creamy cheeses often benefit from the cutting acidity of bright wines, while hard, aged cheeses match well with tannic reds. The bubbles in sparkling wines can beautifully cleanse the palate after rich, creamy cheeses.</p><p>Here are some classic pairings to try:</p><ul><li><strong>Brie + Chardonnay</strong>: The buttery notes in both create a harmonious pairing</li><li><strong>Cheddar + Cabernet Sauvignon</strong>: The wine's tannins cut through the richness of the cheese</li><li><strong>Blue Cheese + Port</strong>: The sweetness of Port balances the cheese's saltiness</li><li><strong>Goat Cheese + Sauvignon Blanc</strong>: The wine's acidity complements the tangy cheese</li><li><strong>Parmesan + Prosecco</strong>: The bubbles and acidity refresh after the salty, umami cheese</li></ul>",
                    ImageUrl = "/images/blog/wine-cheese.jpg",
                    PublishedDate = DateTime.Now.AddDays(-12),
                    Author = "Thomas Sommelier",
                    Tags = new List<string> { "Food Pairing", "Wine Tasting", "Entertaining" }
                },
                new BlogPost
                {
                    Id = 3,
                    Title = "Decoding Wine Labels: What to Look For",
                    Excerpt = "Navigate the sometimes confusing world of wine labels with our comprehensive guide to understanding what they tell you about the wine inside.",
                    Content = "<p>Wine labels can be confusing, filled with unfamiliar terms and varying greatly between countries. Learning to decode them helps you make better purchase decisions and understand what to expect in the bottle.</p><p><strong>Producer/Winery</strong>: Usually the most prominent name on the bottle. Recognizing quality producers is a shortcut to finding good wines.</p><p><strong>Region</strong>: The geographical area where the grapes were grown. This can range from broad (California) to very specific (Rutherford, Napa Valley). More specific regions typically indicate higher quality standards.</p><p><strong>Grape Variety</strong>: Common on New World wines (USA, Australia, etc.) but less so on European bottles. Wines from Europe often emphasize the region rather than the grape, assuming consumers know, for example, that Burgundy means Pinot Noir or Chardonnay.</p><p><strong>Vintage</strong>: The year the grapes were harvested. Important for understanding the potential quality and aging potential, as weather conditions vary yearly.</p><p><strong>Classification/Quality Level</strong>: Terms like 'Grand Cru' (France), 'Reserva' (Spain), or 'DOCG' (Italy) indicate quality classifications specific to each country.</p><p><strong>Alcohol Content</strong>: Usually displayed as percentage by volume. Generally ranges from 11% to 15% for most table wines.</p><p><strong>Special Terms</strong>:</p><ul><li><strong>Estate Bottled</strong>: The producer grew the grapes and made the wine on their property</li><li><strong>Old Vines/Vielles Vignes</strong>: From older grapevines, often producing more concentrated flavors</li><li><strong>Single Vineyard</strong>: All grapes came from one specific vineyard, often indicating higher quality</li></ul><p>Understanding these elements helps you predict a wine's style and quality before even opening the bottle, making your wine shopping experience more informed and enjoyable.</p>",
                    ImageUrl = "/images/blog/wine-label.jpg",
                    PublishedDate = DateTime.Now.AddDays(-20),
                    Author = "Sophia Vino",
                    Tags = new List<string> { "Wine Shopping", "Wine Basics", "Wine Regions" }
                }
            };
        }
    }
}