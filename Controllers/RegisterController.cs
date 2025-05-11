using dotnetprojekt.Context;
using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Models;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;

namespace dotnetprojekt.Controllers
{
    public class RegisterController : Controller
    {

        private readonly WineLoversContext _context;

        // Wstrzyknięcie kontekstu przez konstruktor
        public RegisterController(WineLoversContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        public async Task<IActionResult> Register(UserRegister user)
        {
           
            if (!ModelState.IsValid)
            {
                ViewBag.message = "Please enter correct data";
                return View("/Views/Auth/Register.cshtml");
            }

            var reg = await _context.Users.FirstOrDefaultAsync(x => x.Email == user.Email);
            if( reg != null)
            {
                ViewBag.message = "User with this email already exists";
                return View("/Views/Auth/Register.cshtml");
            }
            User newuser = new User{
                Username = user.Username,
                Password = user.Password,
                Email = user.Email
            };

            _context.Users.Add(newuser);
            await _context.SaveChangesAsync();

            return View("/Views/Auth/Login.cshtml");
        }

    }
}
