using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using System;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DailyArrivals()
        {
            var arrivals = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Where(r => r.CheckInDate.Date == DateTime.Today)
                .ToListAsync();

            return View(arrivals);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string? reservationNumber, string? customerName)
        {
            var reservations = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(reservationNumber)
                && int.TryParse(reservationNumber, out int reservationId))
            {
                reservations = reservations.Where(r => r.ReservationID == reservationId);
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                reservations = reservations.Where(r =>
                    r.User.FirstName.Contains(customerName) ||
                    r.User.LastName.Contains(customerName));
            }

            var results = await reservations.ToListAsync();

            return View("SearchResults", results);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            ViewBag.AvailableSites = await _context.Sites
                .Where(s => s.SiteStatus == SiteStatus.Available.ToString())
                .ToListAsync();

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DateTime checkInDate, DateTime checkOutDate, int? newSiteID)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.CheckInDate = checkInDate;
            reservation.CheckOutDate = checkOutDate;

            if (newSiteID.HasValue)
            {
                reservation.SiteID = newSiteID.Value;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Search));
        }

        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Search));
        }
    }
}