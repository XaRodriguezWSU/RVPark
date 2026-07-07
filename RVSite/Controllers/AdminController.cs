using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using System;
using System.Linq;
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
        public async Task<IActionResult> Dashboard()
        {
            var totalSites = await _context.Sites.CountAsync();

            var availableSites = await _context.Sites
                .CountAsync(s => s.SiteStatus == SiteStatus.Available.ToString());

            var reservedSites = await _context.Sites
                .CountAsync(s => s.SiteStatus == SiteStatus.Reserved.ToString());

            var maintenanceSites = await _context.Sites
                .CountAsync(s => s.SiteStatus == SiteStatus.Maintenance.ToString());

            var recentSites = await _context.Sites
                .OrderBy(s => s.SiteNumber)
                .Take(8)
                .ToListAsync();

            ViewBag.TotalSites = totalSites;
            ViewBag.AvailableSites = availableSites;
            ViewBag.ReservedSites = reservedSites;
            ViewBag.MaintenanceSites = maintenanceSites;
            ViewBag.RecentSites = recentSites;

            return View();
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
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
        public async Task<IActionResult> Edit(
            int id,
            DateTime checkInDate,
            DateTime checkOutDate,
            int? newSiteID)
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

        [HttpGet]
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