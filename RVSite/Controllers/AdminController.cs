using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
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
            var today = DateTime.Today;

            var totalSites = await _context.Sites.CountAsync();

            var reservedSites = await _context.Reservations
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckInDate.Date <= today &&
                    r.CheckOutDate.Date >= today)
                .Select(r => r.SiteID)
                .Distinct()
                .CountAsync();

            var maintenanceTasks = await _context.MaintenanceTasks
                .CountAsync(t =>
                    t.Status == MaintenanceTaskStatus.Open ||
                    t.Status == MaintenanceTaskStatus.InProgress);

            var availableSites = totalSites - reservedSites;

            var recentSites = await _context.Sites
                .Include(s => s.SiteType)
                .OrderBy(s => s.SiteNumber)
                .Take(8)
                .ToListAsync();

            ViewBag.TotalSites = totalSites;
            ViewBag.AvailableSites = availableSites;
            ViewBag.ReservedSites = reservedSites;
            ViewBag.MaintenanceSites = maintenanceTasks;
            ViewBag.RecentSites = recentSites;

            return View();
        }


        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        /// <summary>
        /// Displays the admin/reservation reports dashboard and generates summary metrics for the
        /// selected reporting period. 
        /// </summary>
        /// <param name="startDate">The beginning of reporting period</param>
        /// <param name="endDate">The end of the reporting period</param>
        /// <param name="reportType">The selected report type to display</param>
        /// <returns>The admin/reservations report dashboard view</returns>
        [HttpGet]
        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate, string? reportType)
        {
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today;
            var selectedReportType = reportType ?? "Reservations";

            if (start > end)
            {
                ModelState.AddModelError("", "Start date cannot be after end date.");

                return View(new ReportsViewModel
                {
                    StartDate = start,
                    EndDate = end,
                    ReportType = selectedReportType
                });
            }

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Where(r => r.CheckInDate.Date <= end.Date &&
                            r.CheckOutDate.Date >= start.Date)
                .ToListAsync();

            var nonCancelledReservations = reservations
                .Where(r => r.ReservationStatus != ReservationStatus.Cancelled)
                .ToList();

            var filteredReservations = selectedReportType switch
            {
                "Arrivals" => nonCancelledReservations
                    .Where(r => r.CheckInDate.Date >= start.Date &&
                                r.CheckInDate.Date <= end.Date)
                    .OrderBy(r => r.CheckInDate)
                    .ToList(),

                "Departures" => nonCancelledReservations
                    .Where(r => r.CheckOutDate.Date >= start.Date &&
                                r.CheckOutDate.Date <= end.Date)
                    .OrderBy(r => r.CheckOutDate)
                    .ToList(),

                "Revenue" => nonCancelledReservations
                    .OrderByDescending(r => r.TotalCost)
                    .ToList(),

                "Occupancy" => nonCancelledReservations
                    .OrderBy(r => r.SiteID)
                    .ThenBy(r => r.CheckInDate)
                    .ToList(),

                _ => reservations
                    .OrderBy(r => r.CheckInDate)
                    .ToList()
            };

            var totalSites = await _context.Sites.CountAsync();

            var occupiedSiteCount = nonCancelledReservations
                .Select(r => r.SiteID)
                .Distinct()
                .Count();

            var model = new ReportsViewModel
            {
                StartDate = start,
                EndDate = end,
                ReportType = selectedReportType,

                ArrivalsCount = nonCancelledReservations.Count(r =>
                    r.CheckInDate.Date >= start.Date &&
                    r.CheckInDate.Date <= end.Date),

                DeparturesCount = nonCancelledReservations.Count(r =>
                    r.CheckOutDate.Date >= start.Date &&
                    r.CheckOutDate.Date <= end.Date),

                RevenueTotal = nonCancelledReservations.Sum(r => r.TotalCost),

                TotalSites = totalSites,
                OccupiedSites = occupiedSiteCount,

                OccupancyRate = totalSites == 0
                    ? 0
                    : Math.Round((decimal)occupiedSiteCount / totalSites * 100, 1),

                Reservations = filteredReservations
            };

            return View(model);
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

        // Stuff for the employee management system below
        [HttpGet]
        public async Task<IActionResult> Employees()
        {
            var employees = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Type == RoleType.Staff || u.Role.Type == RoleType.Admin)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEmployee()
        {
            ViewBag.AvailableRoles = await _context.Role
                .Where(r => r.Type == RoleType.Staff || r.Type == RoleType.Admin)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee(
            string firstName, string lastName,
            string email, string phoneNumber, int selectedRoleID, string password)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableRoles = await _context.Role
                    .Where(r => r.Type == RoleType.Staff || r.Type == RoleType.Admin)
                    .ToListAsync();

                return View();
            }

            var employee = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                RoleID = selectedRoleID,
                PasswordHash = password
            };

            _context.Users.Add(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null) return NotFound();

            ViewBag.AvailableRoles = await _context.Role
                .Where(r => r.Type == RoleType.Staff || r.Type == RoleType.Admin)
                .ToListAsync();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(
            int id,
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            int selectedRoleID,
            bool isLocked = false)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;
            user.RoleID = selectedRoleID;
            user.IsLocked = isLocked;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            user.IsLocked = !user.IsLocked;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees));
        }
    }
}