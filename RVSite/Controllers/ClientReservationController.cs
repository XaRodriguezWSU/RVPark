using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using RVSite.Services;
using System.Security.Claims;

namespace RVSite.Controllers
{
    public class ClientReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CostService _costService;

        public ClientReservationController(AppDbContext context, CostService costService)
        {
            _context = context;
            _costService = costService;
        }

        // Step 1: Show booking form
        public IActionResult Book()
        {
            ViewBag.SiteTypes = _context.SiteTypes.ToList();
            return View(new Reservation());
        }

        [HttpPost]
        public IActionResult Book(Reservation model, string siteType, int? rvLength, bool search = false)
        {
            ViewBag.SiteTypes = _context.SiteTypes.ToList();

            if (search)
            {
                // Step 1: User clicked "Search"
                var availableSites = _context.Sites
                    .Where(s => s.SiteType.Name == siteType &&
                                (rvLength == null || s.MaxRVLength >= rvLength))
                    .Where(s => !_context.Reservations.Any(r =>
                        r.SiteID == s.SiteID &&
                        r.CheckInDate < model.CheckOutDate &&
                        model.CheckInDate < r.CheckOutDate))
                    .ToList();

                ViewBag.AvailableSites = availableSites;
                return View(model);
            }

            // Step 2: User clicked "Reserve"
            model.UserID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            model.Site = _context.Sites.FirstOrDefault(s => s.SiteID == model.SiteID);
            model.TotalCost = _costService.CalculateCost(model);
            model.BalanceDue = model.TotalCost;
            model.ReservationStatus = ReservationStatus.Confirmed;

            _context.Reservations.Add(model);
            _context.SaveChanges();

            return RedirectToAction("Confirmation", new { id = model.ReservationID });
        }


        public IActionResult Confirmation(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                .Include(r => r.User)
                .Include(r => r.Site.SiteType)
                .FirstOrDefault(r => r.ReservationID == id);

            Console.WriteLine("Confirmation ID: " + id);
            Console.WriteLine("Reservation is null? " + (reservation == null));
            Console.WriteLine("Site is null? " + (reservation?.Site == null));
            Console.WriteLine("User is null? " + (reservation?.User == null));

            if (reservation == null)
                return NotFound();

            return View(reservation);
        }
    }
}
