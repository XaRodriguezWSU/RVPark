using Microsoft.AspNetCore.Mvc;
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using RVSite.Services;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CostService _costService;
        private readonly ReservationPolicyService _reservationPolicyService;

        public ReservationController(
            AppDbContext context,
            CostService costService,
            ReservationPolicyService reservationPolicyService)
        {
            _context = context;
            _costService = costService;
            _reservationPolicyService = reservationPolicyService;
        }

        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> WalkIn(
            string? customerName,
            string? email,
            string? phoneNumber,
            int? selectedCustomerId)
        {
            var customers = new List<User>();

            bool searchPerformed =
                !string.IsNullOrWhiteSpace(customerName) ||
                !string.IsNullOrWhiteSpace(email) ||
                !string.IsNullOrWhiteSpace(phoneNumber);

            if (searchPerformed)
            {
                var query = _context.Users
                    .Include(u => u.Role)
                    .AsNoTracking()
                    .Where(u =>
                        u.Role != null &&
                        u.Role.Type == RoleType.Customer)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    customerName = customerName.Trim();

                    query = query.Where(u =>
                        u.FirstName.Contains(customerName) ||
                        u.LastName.Contains(customerName) ||
                        (u.FirstName + " " + u.LastName)
                            .Contains(customerName));
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    email = email.Trim();

                    query = query.Where(u =>
                        u.Email.Contains(email));
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    phoneNumber = phoneNumber.Trim();

                    query = query.Where(u =>
                        u.PhoneNumber.Contains(phoneNumber));
                }

                customers = await query
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Take(25)
                    .ToListAsync();
            }

            User? selectedCustomer = null;

            if (selectedCustomerId.HasValue)
            {
                selectedCustomer = await _context.Users
                    .Include(u => u.Role)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.UserID == selectedCustomerId.Value &&
                        u.Role != null &&
                        u.Role.Type == RoleType.Customer);

                if (selectedCustomer == null)
                {
                    return NotFound();
                }
            }

            ViewBag.SearchPerformed = searchPerformed;
            ViewBag.CustomerName = customerName;
            ViewBag.Email = email;
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.SelectedCustomer = selectedCustomer;

            return View(customers);
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(
            string firstName,
            string lastName,
            string email,
            string phoneNumber,
            string password)
        {
            firstName = firstName?.Trim() ?? string.Empty;
            lastName = lastName?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            phoneNumber = phoneNumber?.Trim() ?? string.Empty;
            password = password?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                ModelState.AddModelError(
                    "firstName",
                    "First name is required.");
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                ModelState.AddModelError(
                    "lastName",
                    "Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Email address is required.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError(
                    "phoneNumber",
                    "Phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "password",
                    "Password is required.");
            }

            if (!ModelState.IsValid)
            {
                SetCustomerFormValues(
                    firstName,
                    lastName,
                    email,
                    phoneNumber);

                return View();
            }

            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.Email.ToLower() == email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError(
                    "email",
                    "A user with this email address already exists.");

                SetCustomerFormValues(
                    firstName,
                    lastName,
                    email,
                    phoneNumber);

                return View();
            }

            var customerRole = await _context.Role
                .FirstOrDefaultAsync(r =>
                    r.Type == RoleType.Customer);

            if (customerRole == null)
            {
                ModelState.AddModelError(
                    "",
                    "The Customer role was not found in the database.");

                SetCustomerFormValues(
                    firstName,
                    lastName,
                    email,
                    phoneNumber);

                return View();
            }

            var customer = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                PasswordHash = password,
                RoleID = customerRole.RoleID
            };

            _context.Users.Add(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The new customer was created successfully.";

            return RedirectToAction(
                nameof(WalkIn),
                new
                {
                    selectedCustomerId = customer.UserID
                });
        }

        [HttpGet]
        public async Task<IActionResult> CreateWalkInReservation(
            int customerId,
            DateTime? checkInDate,
            DateTime? checkOutDate)
        {
            var customer = await _context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.UserID == customerId &&
                    u.Role != null &&
                    u.Role.Type == RoleType.Customer);

            if (customer == null)
            {
                return NotFound();
            }

            DateTime selectedCheckIn =
                checkInDate?.Date ?? DateTime.Today;

            DateTime selectedCheckOut =
                checkOutDate?.Date ?? DateTime.Today.AddDays(1);

            ViewBag.Customer = customer;
            ViewBag.CheckInDate = selectedCheckIn;
            ViewBag.CheckOutDate = selectedCheckOut;
            ViewBag.DateSearchPerformed =
                checkInDate.HasValue && checkOutDate.HasValue;

            if (selectedCheckOut <= selectedCheckIn)
            {
                ViewBag.AvailableSites = new List<Site>();
                ViewBag.DateError =
                    "The check-out date must be after the check-in date.";

                return View();
            }

            ViewBag.AvailableSites =
                await GetAvailableSites(
                    selectedCheckIn,
                    selectedCheckOut);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkInReservation(
            int customerId,
            int siteId,
            DateTime checkInDate,
            DateTime checkOutDate,
            int numberOfAdults,
            int numberOfChildren,
            int numberOfPets,
            string? specialRequests)
        {
            var customer = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserID == customerId &&
                    u.Role != null &&
                    u.Role.Type == RoleType.Customer);

            if (customer == null)
            {
                return NotFound();
            }

            checkInDate = checkInDate.Date;
            checkOutDate = checkOutDate.Date;

            ValidateReservationForm(
                checkInDate,
                checkOutDate,
                numberOfAdults,
                numberOfChildren,
                numberOfPets);

            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s =>
                    s.SiteID == siteId);

            if (site == null)
            {
                ModelState.AddModelError(
                    "siteId",
                    "Please select a valid site.");
            }
            else if (site.SiteStatus ==
                     SiteStatus.Maintenance.ToString())
            {
                ModelState.AddModelError(
                    "siteId",
                    "The selected site is currently under maintenance.");
            }

            if (site != null &&
                checkOutDate > checkInDate)
            {
                bool siteHasConflict =
                    await _context.Reservations.AnyAsync(r =>
                        r.SiteID == siteId &&
                        r.ReservationStatus !=
                            ReservationStatus.Cancelled &&
                        r.CheckInDate < checkOutDate &&
                        checkInDate < r.CheckOutDate);

                if (siteHasConflict)
                {
                    ModelState.AddModelError(
                        "siteId",
                        "The selected site is no longer available for these dates.");
                }
            }

            if (ModelState.IsValid &&
                site != null &&
                checkOutDate > checkInDate)
            {
                var policyValidation =
                    await _reservationPolicyService.ValidateReservationAsync(
                        checkInDate,
                        checkOutDate,
                        customerId,
                        siteId);

                if (!policyValidation.IsValid)
                {
                    foreach (var error in policyValidation.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }

            if (!ModelState.IsValid || site == null)
            {
                ViewBag.Customer = customer;
                ViewBag.CheckInDate = checkInDate;
                ViewBag.CheckOutDate = checkOutDate;
                ViewBag.NumberOfAdults = numberOfAdults;
                ViewBag.NumberOfChildren = numberOfChildren;
                ViewBag.NumberOfPets = numberOfPets;
                ViewBag.SpecialRequests = specialRequests;
                ViewBag.DateSearchPerformed = true;

                if (checkOutDate > checkInDate)
                {
                    ViewBag.AvailableSites =
                        await GetAvailableSites(
                            checkInDate,
                            checkOutDate);
                }
                else
                {
                    ViewBag.AvailableSites =
                        new List<Site>();
                }

                return View();
            }

            var reservation = new Reservation
            {
                UserID = customerId,
                User = customer,
                SiteID = siteId,
                Site = site,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                NumberOfPets = numberOfPets,
                SpecialRequests =
                    string.IsNullOrWhiteSpace(specialRequests)
                        ? null
                        : specialRequests.Trim(),
                ReservationStatus =
                    ReservationStatus.Confirmed,
                ReservationDate = DateTime.Now
            };

            decimal totalCost =
                _costService.CalculateCost(reservation);

            reservation.TotalCost = totalCost;
            reservation.BalanceDue = totalCost;

            site.SiteStatus =
                SiteStatus.Reserved.ToString();

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The walk-in reservation was created successfully.";

            return RedirectToAction(
                nameof(WalkInConfirmation),
                new
                {
                    id = reservation.ReservationID
                });
        }

        [HttpGet]
        public async Task<IActionResult> WalkInConfirmation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(
            string? reservationNumber,
            string? customerName)
        {
            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(reservationNumber) &&
                int.TryParse(
                    reservationNumber,
                    out int reservationId))
            {
                query = query.Where(r =>
                    r.ReservationID == reservationId);
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                customerName = customerName.Trim();

                query = query.Where(r =>
                    r.User != null &&
                    (
                        r.User.FirstName.Contains(customerName) ||
                        r.User.LastName.Contains(customerName) ||
                        (r.User.FirstName + " " + r.User.LastName)
                            .Contains(customerName)
                    ));
            }

            var results = await query.ToListAsync();

            return View(
                "SearchResults",
                results);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            ViewBag.AvailableSites = await _context.Sites
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Reservation updated,
            int? newSiteID,
            bool overrideConflict = false)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Site)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == updated.ReservationID);

            if (reservation == null)
            {
                return NotFound();
            }

            updated.CheckInDate = updated.CheckInDate.Date;
            updated.CheckOutDate = updated.CheckOutDate.Date;

            if (updated.CheckOutDate <= updated.CheckInDate)
            {
                ModelState.AddModelError(
                    "",
                    "The check-out date must be after the check-in date.");

                await PrepareEditView(
                    reservation,
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    newSiteID,
                    false);

                return View(reservation);
            }

            int targetSiteID = newSiteID ?? reservation.SiteID;

            var targetSite = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.SiteID == targetSiteID);

            if (targetSite == null)
            {
                ModelState.AddModelError(
                    "newSiteID",
                    "The selected site could not be found.");

                await PrepareEditView(
                    reservation,
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    newSiteID,
                    false);

                return View(reservation);
            }

            if (targetSite.SiteStatus == SiteStatus.Maintenance.ToString())
            {
                ModelState.AddModelError(
                    "newSiteID",
                    "A site under maintenance cannot be assigned to a reservation.");

                await PrepareEditView(
                    reservation,
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    newSiteID,
                    false);

                return View(reservation);
            }

            var policyValidation =
                await _reservationPolicyService.ValidateReservationAsync(
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    reservation.UserID,
                    targetSiteID,
                    reservation.ReservationID);

            if (!policyValidation.IsValid)
            {
                foreach (var error in policyValidation.Errors)
                {
                    ModelState.AddModelError("", error);
                }

                await PrepareEditView(
                    reservation,
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    newSiteID,
                    false);

                return View(reservation);
            }

            bool hasConflict = await _context.Reservations
                .AnyAsync(r =>
                    r.SiteID == targetSiteID &&
                    r.ReservationID != reservation.ReservationID &&
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckInDate < updated.CheckOutDate &&
                    updated.CheckInDate < r.CheckOutDate);

            if (hasConflict && !overrideConflict)
            {
                ModelState.AddModelError(
                    "",
                    "This site has a reservation conflict for the selected dates. Administrators may review the warning and select Override Reservation Conflict to continue.");

                await PrepareEditView(
                    reservation,
                    updated.CheckInDate,
                    updated.CheckOutDate,
                    newSiteID,
                    true);

                return View(reservation);
            }

            int previousSiteID = reservation.SiteID;
            Site? previousSite = reservation.Site;
            decimal oldTotal = reservation.TotalCost;

            reservation.SiteID = targetSiteID;
            reservation.Site = targetSite;
            reservation.CheckInDate = updated.CheckInDate;
            reservation.CheckOutDate = updated.CheckOutDate;

            decimal newTotal = _costService.CalculateCost(reservation);
            decimal balanceDifference = newTotal - oldTotal;

            reservation.TotalCost = newTotal;
            reservation.BalanceDue += balanceDifference;

            targetSite.SiteStatus = SiteStatus.Reserved.ToString();

            if (previousSite != null && previousSiteID != targetSiteID)
            {
                bool previousSiteStillNeeded = await _context.Reservations
                    .AnyAsync(r =>
                        r.SiteID == previousSiteID &&
                        r.ReservationID != reservation.ReservationID &&
                        r.ReservationStatus != ReservationStatus.Cancelled);

                if (!previousSiteStillNeeded &&
                    previousSite.SiteStatus != SiteStatus.Maintenance.ToString())
                {
                    previousSite.SiteStatus = SiteStatus.Available.ToString();
                }
            }

            ViewBag.BalanceDifference = balanceDifference;
            ViewBag.ConflictOverridden = hasConflict && overrideConflict;

            await _context.SaveChangesAsync();

            return View(
                "EditConfirmation",
                reservation);
        }
        [HttpGet]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCancelReservation(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.ReservationStatus != ReservationStatus.Cancelled)
            {
                reservation.ReservationStatus =
                    ReservationStatus.Cancelled;

                bool anotherActiveReservationExists =
                    await _context.Reservations.AnyAsync(r =>
                        r.SiteID == reservation.SiteID &&
                        r.ReservationID != reservation.ReservationID &&
                        r.ReservationStatus != ReservationStatus.Cancelled);

                if (reservation.Site != null &&
                    !anotherActiveReservationExists)
                {
                    reservation.Site.SiteStatus =
                        SiteStatus.Available.ToString();
                }

                await _context.SaveChangesAsync();
            }

            return View(
                "CancelConfirmation",
                reservation);
        }

        private async Task PrepareEditView(
            Reservation reservation,
            DateTime checkInDate,
            DateTime checkOutDate,
            int? selectedSiteID,
            bool conflictDetected)
        {
            reservation.CheckInDate = checkInDate;
            reservation.CheckOutDate = checkOutDate;

            ViewBag.AvailableSites = await _context.Sites
                .Include(s => s.SiteType)
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            ViewBag.SelectedSiteID = selectedSiteID;
            ViewBag.ConflictDetected = conflictDetected;
        }

        private async Task<List<Site>> GetAvailableSites(
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            return await _context.Sites
                .Include(s => s.SiteType)
                .Where(s =>
                    s.SiteStatus !=
                        SiteStatus.Maintenance.ToString() &&
                    !_context.Reservations.Any(r =>
                        r.SiteID == s.SiteID &&
                        r.ReservationStatus !=
                            ReservationStatus.Cancelled &&
                        r.CheckInDate < checkOutDate &&
                        checkInDate < r.CheckOutDate))
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();
        }

        private void ValidateReservationForm(
            DateTime checkInDate,
            DateTime checkOutDate,
            int numberOfAdults,
            int numberOfChildren,
            int numberOfPets)
        {
            if (checkInDate < DateTime.Today)
            {
                ModelState.AddModelError(
                    "checkInDate",
                    "The check-in date cannot be in the past.");
            }

            if (checkOutDate <= checkInDate)
            {
                ModelState.AddModelError(
                    "checkOutDate",
                    "The check-out date must be after the check-in date.");
            }

            if (numberOfAdults < 1 ||
                numberOfAdults > 20)
            {
                ModelState.AddModelError(
                    "numberOfAdults",
                    "The number of adults must be between 1 and 20.");
            }

            if (numberOfChildren < 0 ||
                numberOfChildren > 20)
            {
                ModelState.AddModelError(
                    "numberOfChildren",
                    "The number of children must be between 0 and 20.");
            }

            if (numberOfPets < 0 ||
                numberOfPets > 20)
            {
                ModelState.AddModelError(
                    "numberOfPets",
                    "The number of pets must be between 0 and 20.");
            }
        }

        private void SetCustomerFormValues(
            string firstName,
            string lastName,
            string email,
            string phoneNumber)
        {
            ViewBag.FirstName = firstName;
            ViewBag.LastName = lastName;
            ViewBag.Email = email;
            ViewBag.PhoneNumber = phoneNumber;
        }
    }
}
