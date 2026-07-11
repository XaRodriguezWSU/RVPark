using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using System.Diagnostics;

namespace RVSite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sites = await _context.Sites
                .Include(s => s.SiteType)
                .Include(s => s.Photos)
                .Where(s => s.SiteStatus == SiteStatus.Available.ToString())
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            return View(sites);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}