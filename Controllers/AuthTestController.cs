using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace dotnetprojekt.Controllers
{
    public class AuthTestController : Controller
    {
        private readonly ILogger<AuthTestController> _logger;

        public AuthTestController(ILogger<AuthTestController> logger)
        {
            _logger = logger;
        }

        // Main test page with instructions and links
        public IActionResult Index()
        {
            _logger.LogInformation("Auth test index page accessed");
            return View();
        }

        // Public endpoint that anyone can access
        public IActionResult Public()
        {
            _logger.LogInformation("Public endpoint accessed");
            return Content("This is a public endpoint that doesn't require authentication.");
        }

        // Protected endpoint that requires authentication
        [Authorize]
        public IActionResult Protected()
        {
            _logger.LogInformation("Protected endpoint accessed by authenticated user");
            return Content("If you can see this, you are authenticated!");
        }

        // Test endpoint that specifically logs authentication info
        public IActionResult TestAuth()
        {
            _logger.LogInformation("Auth test endpoint accessed");
            
            // Log authentication information
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("User authenticated: {IsAuthenticated}", isAuthenticated);
            
            if (isAuthenticated)
            {
                _logger.LogInformation("User claims:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim type: {ClaimType}, Value: {ClaimValue}", 
                        claim.Type, claim.Value);
                }
            }
            
            // Log all cookies
            _logger.LogInformation("Cookies in request:");
            foreach (var cookie in Request.Cookies)
            {
                _logger.LogInformation("Cookie: {CookieName}", cookie.Key);
            }
            
            return Content($"Authentication Test: IsAuthenticated={isAuthenticated}");
        }
    }
}