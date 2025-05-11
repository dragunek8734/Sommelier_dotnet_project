using dotnetprojekt.Authentication;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace dotnetprojekt.Controllers
{
    public class LoginController : Controller
    {
        private readonly WineLoversContext _context;
        private readonly JwtProvider _jwtProvider;
        private readonly ILogger<LoginController> _logger;

        // Wstrzyknięcie kontekstu przez konstruktor
        public LoginController(WineLoversContext context, JwtProvider jwtProvider, ILogger<LoginController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _jwtProvider = jwtProvider ?? throw new ArgumentNullException(nameof(jwtProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> Verify(User user)
        {
            string message;
            string ipAddress = GetUserIpAddress();
            string userAgent = Request.Headers["User-Agent"].ToString();
            string device = GetDeviceInfo(userAgent);
            int? userId = null;

            if(string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                message = "Please enter correct username and password";
                
                // Record failed login attempt due to empty credentials
                await LogLoginAttempt(userId, ipAddress, userAgent, device, false, "Empty credentials");

                ViewBag.message = message;
                return View("/Views/Auth/Login.cshtml");
            }

            var reg = await _context.Users.FirstOrDefaultAsync(x => x.Username == user.Username && x.Password == user.Password);
            
            if (reg == null)
            {
                message = "Invalid username or password, try again.";
                
                // Try to find user by username to record the userId for the failed attempt
                var userByUsername = await _context.Users.FirstOrDefaultAsync(x => x.Username == user.Username);
                userId = userByUsername?.Id;
                
                // Record failed login attempt
                await LogLoginAttempt(userId, ipAddress, userAgent, device, false, "Invalid credentials");
                
                ViewBag.message = message;
                return View("Views/Auth/Login.cshtml");
            }
            
            // User successfully authenticated
            userId = reg.Id;
            
            // Record successful login attempt
            await LogLoginAttempt(userId, ipAddress, userAgent, device, true, null);

            {
                // Generate JWT token
                var token = _jwtProvider.GenerateJwtToken(reg);
                
                // Better cookie configuration that works with both HTTP and HTTPS
                Response.Cookies.Append("jwt", token, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax, // Changed from None to Lax for better browser compatibility
                    Secure = Request.IsHttps,     // Only set Secure flag when using HTTPS
                    Expires = DateTime.UtcNow.AddMinutes(60),
                    Path = "/"                    // Ensure cookie is sent with all requests to the domain
                });
                
                _logger.LogInformation($"Token generated for user: {reg.Username}");
                _logger.LogDebug($"Token: {token.Substring(0, 20)}...");

                return RedirectToAction("Index", "Home");
            }
            
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("jwt");

            return RedirectToAction("Index", "Home");
        }
        
        // Helper method to log login attempts
        private async Task LogLoginAttempt(int? userId, string ipAddress, string userAgent, string device, bool success, string failureReason)
        {
            try
            {
                var loginRecord = new LoginHistory
                {
                    UserId = userId ?? 0, // If userId is null (not found), use 0 as a placeholder
                    LoginTime = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Device = device,
                    Success = success,
                    FailureReason = failureReason ?? string.Empty
                };
                
                // Only add to database if we have a valid user
                if (userId.HasValue && userId.Value > 0)
                {
                    _context.LoginHistory.Add(loginRecord);
                    await _context.SaveChangesAsync();
                }
                
                // Always log the attempt
                if (success)
                {
                    _logger.LogInformation($"Successful login for user ID: {userId}, IP: {ipAddress}, Device: {device}");
                }
                else
                {
                    _logger.LogWarning($"Failed login attempt for {(userId.HasValue ? $"user ID: {userId}" : "unknown user")}, IP: {ipAddress}, Device: {device}, Reason: {failureReason}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error logging login attempt for user ID: {userId}");
            }
        }
        
        // Helper method to get user's IP address
        private string GetUserIpAddress()
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            // If behind a proxy like Cloudflare or running behind a load balancer
            string forwardedIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                ip = forwardedIp.Split(',').FirstOrDefault()?.Trim() ?? ip;
            }
            
            return ip;
        }
        
        // Helper method to parse device info from User-Agent
        private string GetDeviceInfo(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";
                
            // Simple device detection - can be expanded with a more sophisticated library
            if (userAgent.Contains("Mobile") || userAgent.Contains("Android"))
                return "Mobile";
            else if (userAgent.Contains("iPad") || userAgent.Contains("Tablet"))
                return "Tablet";
            else
                return "Desktop";
                
            // Alternative: Return browser and OS info
            // string device = "Unknown";
            // Match browserMatch = Regex.Match(userAgent, @"(Chrome|Safari|Firefox|Edge|MSIE|Trident)");
            // Match osMatch = Regex.Match(userAgent, @"(Windows|Mac|iOS|Android|Linux)");
            // if (browserMatch.Success)
            //     device = browserMatch.Groups[1].Value;
            // if (osMatch.Success)
            //     device += " on " + osMatch.Groups[1].Value;
            // return device;
        }
    }
}
