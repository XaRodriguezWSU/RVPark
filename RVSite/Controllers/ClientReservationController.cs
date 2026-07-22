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
        private readonly ReservationPolicyService _reservationPolicyService;

        public ClientReservationController(AppDbContext context, CostService costService, ReservationPolicyService reservationPolicyService)
        {
            _context = context;
            _costService = costService;
            _reservationPolicyService = reservationPolicyService;
        }

        // Step 1: Show booking form
        public IActionResult Book()
        {
            ViewBag.SiteTypes = _context.SiteTypes.ToList();
            return View(new Reservation());
        }

        [HttpPost]
        public async Task<IActionResult> Book(Reservation model, string? siteType, int? rvLength, bool search = false)
        {
            ViewBag.SiteTypes = _context.SiteTypes.ToList();
            ViewBag.SelectedSiteType = siteType;
            ViewBag.RvLength = rvLength;

            if (search)
            {
                // Step 1: User clicked "Search"
                ModelState.Remove(nameof(Reservation.SiteID));
                ModelState.Remove(nameof(Reservation.NumberOfAdults));
                ModelState.Remove(nameof(Reservation.NumberOfChildren));
                ModelState.Remove(nameof(Reservation.NumberOfPets));
                ModelState.Remove(nameof(Reservation.SpecialRequests));

                model.CheckInDate = model.CheckInDate.Date;
                model.CheckOutDate = model.CheckOutDate.Date;

                if (string.IsNullOrWhiteSpace(siteType))
                {
                    ModelState.AddModelError("siteType", "Please select a site type.");
                }

                if (model.CheckInDate < DateTime.Today)
                {
                    ModelState.AddModelError(nameof(Reservation.CheckInDate), "The check-in date cannot be in the past.");
                }

                if (model.CheckOutDate <= model.CheckInDate)
                {
                    ModelState.AddModelError(nameof(Reservation.CheckOutDate), "The check-out date must be after the check-in date.");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // get available sites
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

            // Step 3: Confirm reservation meets policy requirements
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
            {
                return Challenge();
            }

            model.UserID = userId;
            model.CheckInDate = model.CheckInDate.Date;
            model.CheckOutDate = model.CheckOutDate.Date;

            var policyValidation = await _reservationPolicyService.ValidateReservationAsync(
                model.CheckInDate,
                model.CheckOutDate,
                model.UserID,
                model.SiteID);

            if (!policyValidation.IsValid)
            {
                foreach (var error in policyValidation.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                ViewBag.SiteTypes = _context.SiteTypes.ToList();
                return View(model);
            }

            // Step 4: Calculate cost and save reservation
            model.TotalCost = _costService.CalculateCost(model);
            model.BalanceDue = model.TotalCost;
            model.ReservationStatus = ReservationStatus.Pending;

            _context.Reservations.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Checkout", "Payment", new { ReservationId = model.ReservationID });
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

        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationID == id);

            if (reservation == null)
                return NotFound();

            // Only allow editing future reservations
            if (reservation.CheckInDate <= DateTime.Today)
                return BadRequest("Past reservations cannot be edited.");

            ViewBag.SiteTypes = _context.SiteTypes.ToList();

            return View(reservation);
        }

        [HttpPost]
        public IActionResult Edit(Reservation updated, string siteType, int? rvLength)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationID == updated.ReservationID);

            if (reservation == null)
                return NotFound();

            // 1. Validate new dates
            if (updated.CheckInDate >= updated.CheckOutDate)
            {
                ModelState.AddModelError("", "Check-out must be after check-in.");
                ViewBag.SiteTypes = _context.SiteTypes.ToList();
                return View(reservation);
            }

            // 2. Find available sites matching new criteria
            var availableSites = _context.Sites
                .Where(s => s.SiteType.Name == siteType &&
                            (rvLength == null || s.MaxRVLength >= rvLength))
                .Where(s => !_context.Reservations.Any(r =>
                    r.SiteID == s.SiteID &&
                    r.ReservationID != reservation.ReservationID &&
                    r.CheckInDate < updated.CheckOutDate &&
                    updated.CheckInDate < r.CheckOutDate))
                .ToList();

            if (!availableSites.Any())
            {
                ModelState.AddModelError("", "No sites available for the updated criteria.");
                ViewBag.SiteTypes = _context.SiteTypes.ToList();
                ViewBag.AvailableSites = availableSites;
                return View(reservation);
            }

            // 3. Update reservation fields
            reservation.CheckInDate = updated.CheckInDate;
            reservation.CheckOutDate = updated.CheckOutDate;
            reservation.NumberOfAdults = updated.NumberOfAdults;
            reservation.NumberOfChildren = updated.NumberOfChildren;
            reservation.NumberOfPets = updated.NumberOfPets;
            reservation.SpecialRequests = updated.SpecialRequests;

            // 4. Update site if changed
            if (updated.SiteID > 0 && updated.SiteID != reservation.SiteID)
            {
                var newSite = _context.Sites
                    .Include(s => s.SiteType)
                    .FirstOrDefault(s => s.SiteID == updated.SiteID);

                if (newSite == null)
                {
                    ModelState.AddModelError("", "The selected site could not be found.");
                    ViewBag.SiteTypes = _context.SiteTypes.ToList();
                    return View(reservation);
                }

                reservation.SiteID = updated.SiteID;
                reservation.Site = newSite;
            }
            else
            {
                _context.Entry(reservation)
                    .Reference(r => r.Site)
                    .Load();

                if (reservation.Site != null)
                {
                    _context.Entry(reservation.Site)
                        .Reference(s => s.SiteType)
                        .Load();
                }
            }

            // 5. Recalculate cost
            decimal oldTotal = reservation.TotalCost;
            decimal newTotal = _costService.CalculateCost(reservation);

            reservation.TotalCost = newTotal;
            reservation.BalanceDue = newTotal; // No Stripe logic required

            ViewBag.PriceDifference = newTotal - oldTotal;

            _context.SaveChanges();

            return View("EditConfirmation", reservation);
        }

        public IActionResult Cancel(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationID == id);

            if (reservation == null)
                return NotFound();

            if (reservation.CheckInDate <= DateTime.Today)
                return BadRequest("Past reservations cannot be cancelled.");

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _context.SaveChanges();

            return View("CancelConfirmation", reservation);
        }

        public async Task<IActionResult> MyReservations()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var reservationIds = await _context.Reservations
                .Where(r => r.UserID == userId)
                .Select(r => r.ReservationID)
                .ToListAsync();

            foreach (var reservationId in reservationIds)
            {
                await _costService.UpdateReservationBalanceDueAsync(reservationId);
            }

            await _context.SaveChangesAsync();

            var reservations = await _context.Reservations
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .Include(r => r.Fees)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }

    }
}
