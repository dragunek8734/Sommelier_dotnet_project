using dotnetprojekt.FilterAttributes;
using Microsoft.AspNetCore.Mvc;

namespace dotnetprojekt.Controllers
{
    public class AuthController : Controller
    {
        [RedirectIfAuthenticatedAttribute]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [RedirectIfAuthenticatedAttribute]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}
