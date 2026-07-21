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
                    .ThenInclude(s => s.SiteType)
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

                "SiteUsage" => nonCancelledReservations
                    .OrderBy(r => r.Site != null ? r.Site.SiteNumber : "")
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

            var sites = await _context.Sites
                .Include(s => s.SiteType)
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            var reportEndExclusive = end.Date.AddDays(1);

            var siteUsageRows = sites.Select(site =>
            {
                var siteReservations = nonCancelledReservations
                    .Where(r => r.SiteID == site.SiteID)
                    .ToList();

                return new SiteUsageReportRow
                {
                    SiteID = site.SiteID,
                    SiteNumber = site.SiteNumber,
                    SiteTypeName = site.SiteType != null ? site.SiteType.Name : "Unknown",
                    ReservationCount = siteReservations.Count,
                    ReservedNights = siteReservations.Sum(r =>
                    {
                        var overlapStart = r.CheckInDate.Date > start.Date
                            ? r.CheckInDate.Date
                            : start.Date;

                        var overlapEnd = r.CheckOutDate.Date < reportEndExclusive
                            ? r.CheckOutDate.Date
                            : reportEndExclusive;

                        var nights = (overlapEnd - overlapStart).Days;

                        return nights < 0 ? 0 : nights;
                    }),
                    RevenueTotal = siteReservations.Sum(r => r.TotalCost)
                };
            }).ToList();

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

                Reservations = filteredReservations,

                SiteUsageRows = siteUsageRows
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

        private async Task<ReservationPolicy> GetOrCreateReservationPolicyAsync()
        {
            var policy = await _context.ReservationPolicies.FirstOrDefaultAsync();

            if (policy == null)
            {
                policy = new ReservationPolicy();
                _context.ReservationPolicies.Add(policy);
                await _context.SaveChangesAsync();
            }

            return policy;
        }

        [HttpGet]
        public async Task<IActionResult> ReservationPolicies()
        {
            var policy = await GetOrCreateReservationPolicyAsync();

            ViewBag.ActiveSpecialEventPolicies = await _context.SpecialEventPolicies
                .Include(p => p.SiteType)
                .Where(p => p.IsActive && p.EndDate.Date >= DateTime.Today)
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            return View(policy);
        }

        [HttpGet]
        public async Task<IActionResult> EditReservationPolicy()
        {
            var policy = await GetOrCreateReservationPolicyAsync();

            return View(policy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReservationPolicy(ReservationPolicy policy)
        {
            if (!ModelState.IsValid)
            {
                return View(policy);
            }

            var existingPolicy = await _context.ReservationPolicies
                .FirstOrDefaultAsync(p => p.ReservationPolicyID == policy.ReservationPolicyID);

            if (existingPolicy == null)
            {
                return NotFound();
            }

            existingPolicy.MaximumAdvanceBookingDays = policy.MaximumAdvanceBookingDays;
            existingPolicy.PeakSeasonMaximumStayNights = policy.PeakSeasonMaximumStayNights;
            existingPolicy.PeakSeasonStartMonth = policy.PeakSeasonStartMonth;
            existingPolicy.PeakSeasonStartDay = policy.PeakSeasonStartDay;
            existingPolicy.PeakSeasonEndMonth = policy.PeakSeasonEndMonth;
            existingPolicy.PeakSeasonEndDay = policy.PeakSeasonEndDay;
            existingPolicy.RequiredDaysAwayBeforeReturn = policy.RequiredDaysAwayBeforeReturn;
            existingPolicy.LateCancellationWindowDays = policy.LateCancellationWindowDays;
            existingPolicy.GeneralPolicyNotes = policy.GeneralPolicyNotes;
            existingPolicy.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation policies were updated.";

            return RedirectToAction(nameof(ReservationPolicies));
        }

        [HttpGet]
        public async Task<IActionResult> SpecialEventPolicies(bool showArchived = false)
        {
            var today = DateTime.Today;

            var query = _context.SpecialEventPolicies
                .Include(p => p.SiteType)
                .AsQueryable();

            if (showArchived)
            {
                query = query.Where(p => !p.IsActive || p.EndDate.Date < today);
                ViewBag.PageTitle = "Archived Special Event Policies";
            }
            else
            {
                query = query.Where(p => p.IsActive && p.EndDate.Date >= today);
                ViewBag.PageTitle = "Active & Upcoming Special Event Policies";
            }

            ViewBag.ShowArchived = showArchived;

            var policies = showArchived
                ? await query.OrderByDescending(p => p.EndDate).ToListAsync()
                : await query.OrderBy(p => p.StartDate).ToListAsync();

            return View(policies);
        }

        [HttpGet]
        public async Task<IActionResult> SpecialEventPolicyForm(int? id)
        {
            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (id == null)
            {
                return View(new SpecialEventPolicy
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today,
                    IsActive = true
                });
            }

            var policy = await _context.SpecialEventPolicies
                .FirstOrDefaultAsync(p => p.SpecialEventPolicyID == id);

            if (policy == null)
            {
                return NotFound();
            }

            return View(policy);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSpecialEventPolicy(SpecialEventPolicy policy)
        {
            ModelState.Remove(nameof(SpecialEventPolicy.SiteType));

            if (policy.StartDate > policy.EndDate)
            {
                ModelState.AddModelError("", "Start date cannot be after end date.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SiteTypes = await _context.SiteTypes
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return View("SpecialEventPolicyForm", policy);
            }

            if (policy.SpecialEventPolicyID == 0)
            {
                _context.SpecialEventPolicies.Add(policy);
                TempData["SuccessMessage"] = "Special event policy was added.";
            }
            else
            {
                var existingPolicy = await _context.SpecialEventPolicies
                    .FirstOrDefaultAsync(p => p.SpecialEventPolicyID == policy.SpecialEventPolicyID);

                if (existingPolicy == null)
                {
                    return NotFound();
                }

                existingPolicy.EventName = policy.EventName;
                existingPolicy.StartDate = policy.StartDate;
                existingPolicy.EndDate = policy.EndDate;
                existingPolicy.SiteTypeID = policy.SiteTypeID;
                existingPolicy.MaximumStayNights = policy.MaximumStayNights;
                existingPolicy.CancellationWindowDays = policy.CancellationWindowDays;
                existingPolicy.IsActive = policy.IsActive;
                existingPolicy.Notes = policy.Notes;

                TempData["SuccessMessage"] = "Special event policy was updated.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(SpecialEventPolicies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveSpecialEventPolicy(int id)
        {
            var policy = await _context.SpecialEventPolicies.FindAsync(id);

            if (policy == null)
            {
                return NotFound();
            }

            policy.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Special event policy was archived.";

            return RedirectToAction(nameof(SpecialEventPolicies));
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