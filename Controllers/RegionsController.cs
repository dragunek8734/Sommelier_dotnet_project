using Microsoft.AspNetCore.Mvc;
using dotnetprojekt.Context;
using dotnetprojekt.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace dotnetprojekt.Controllers
{
    public class RegionsController : Controller
    {
        private readonly WineLoversContext _context;

        public RegionsController(WineLoversContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var countries = await _context.Countries
                .Include(c => c.Regions)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(countries);
        }
        
        // Nowa akcja dla wyświetlania regionów kraju
        public async Task<IActionResult> CountryDetails(int id)
        {
            var country = await _context.Countries
                .Include(c => c.Regions)
                .ThenInclude(r => r.Wineries)
                .ThenInclude(w => w.Wines)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
            {
                return NotFound();
            }

            return View(country);
        }

        public async Task<IActionResult> RegionDetails(int id)
        {
            var region = await _context.Regions
                .Include(r => r.Country)
                .Include(r => r.Wineries)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (region == null)
            {
                return NotFound();
            }

            return View(region);
        }
    }
}