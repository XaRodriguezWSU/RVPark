using Microsoft.AspNetCore.Mvc;
using RVSite.Models;
using Microsoft.EntityFrameworkCore;
using RVSite.Services;


namespace RVSite.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CostService _costService;

        public ReservationController(AppDbContext context, CostService costService)
        {
            _context = context;
            _costService = costService;
        }

        public IActionResult Index()
        {
            var reservations = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .ToList();

            return View(reservations);
        }

        // Search page - GET
        public IActionResult Search()
        {
            //return View("~/Views/Admin/ReservationView/Search.cshtml");
            return View();
        }

        // Search action - POST
        [HttpPost]
        public IActionResult Search(string reservationNumber, string customerName)
        {
            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .AsQueryable();

            if (!string.IsNullOrEmpty(reservationNumber))
            {
                int resNum = int.Parse(reservationNumber);
                query = query.Where(r => r.ReservationID == resNum);
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                query = query.Where(r => r.User.FirstName.Contains(customerName) || r.User.LastName.Contains(customerName));
            }

            var results = query.ToList();

            return View("SearchResults", results);
        }

        // Edit Reservation - GET
        public IActionResult Edit(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .FirstOrDefault(r => r.ReservationID == id);

            if (reservation == null)
                return NotFound();

            ViewBag.AvailableSites = _context.Sites.ToList();

            return View(reservation);
        }

        // Edit Reservation - POST
        [HttpPost]
        public IActionResult Edit(Reservation updated, int? newSiteID)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationID == updated.ReservationID);

            if (reservation == null)
                return NotFound();

            // 1. Check site availability if site changed
            if (newSiteID.HasValue)
            {
                bool available = !_context.Reservations.Any(r =>
                    r.SiteID == newSiteID.Value &&
                    r.ReservationID != reservation.ReservationID &&
                    r.CheckInDate < updated.CheckOutDate &&
                    updated.CheckInDate < r.CheckOutDate);

                if (!available)
                {
                    ModelState.AddModelError("", "Selected site is not available for these dates.");
                    return View(reservation);
                }

                reservation.SiteID = newSiteID.Value;
                _context.Entry(reservation).Reference(r => r.Site).Load();
                _context.Entry(reservation.Site).Reference(s => s.SiteType).Load();
            }

            // 2. Update dates
            reservation.CheckInDate = updated.CheckInDate;
            reservation.CheckOutDate = updated.CheckOutDate;

            // 3. Calculate balance difference
            decimal oldTotal = reservation.TotalCost;
            decimal newTotal = _costService.CalculateCost(reservation);

            reservation.TotalCost = newTotal;

            ViewBag.BalanceDifference = newTotal - oldTotal;

            _context.SaveChanges();

            return View("EditConfirmation", reservation);
        }

        // Cancel Reservation - GET (?)
        public IActionResult CancelReservation(int id)
        {
            var reservation = _context.Reservations
                .Include(r => r.Site)
                .Include(r => r.User)
                .FirstOrDefault(r => r.ReservationID == id);

            if (reservation == null)
                return NotFound();

            reservation.ReservationStatus = ReservationStatus.Cancelled;

            _context.SaveChanges();

            return View("CancelConfirmation", reservation);
        }

    }
}
