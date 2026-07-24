using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SitesController : Controller
    {
        private readonly AppDbContext _context;

        public SitesController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // PUBLIC CAMPSITE SEARCH
        // ---------------------------------------------------------

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Search()
        {
            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(st => st.Name)
                .ToListAsync();

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> SearchResults(
            DateTime? checkInDate,
            DateTime? checkOutDate,
            int? siteTypeId,
            int? maxRVLength)
        {
            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(st => st.Name)
                .ToListAsync();

            ViewBag.CheckInDate = checkInDate;
            ViewBag.CheckOutDate = checkOutDate;
            ViewBag.SiteTypeId = siteTypeId;
            ViewBag.MaxRVLength = maxRVLength;

            if (checkInDate.HasValue &&
                checkOutDate.HasValue &&
                checkOutDate.Value.Date <= checkInDate.Value.Date)
            {
                ModelState.AddModelError(
                    "",
                    "The check-out date must be after the check-in date.");

                return View(
                    "Search",
                    new List<Site>());
            }

            var sitesQuery = _context.Sites
                .Include(s => s.SiteType)
                .Include(s => s.Photos)
                .Where(s =>
                    s.SiteStatus == SiteStatus.Available.ToString())
                .AsQueryable();

            if (siteTypeId.HasValue)
            {
                sitesQuery = sitesQuery.Where(s =>
                    s.SiteTypeID == siteTypeId.Value);
            }

            if (maxRVLength.HasValue)
            {
                sitesQuery = sitesQuery.Where(s =>
                    s.MaxRVLength >= maxRVLength.Value);
            }

            /*
             * A site is unavailable when an existing, non-cancelled
             * reservation overlaps the requested date range.
             *
             * Existing check-in < requested check-out
             * Existing check-out > requested check-in
             */
            if (checkInDate.HasValue && checkOutDate.HasValue)
            {
                DateTime requestedCheckIn =
                    checkInDate.Value.Date;

                DateTime requestedCheckOut =
                    checkOutDate.Value.Date;

                sitesQuery = sitesQuery.Where(site =>
                    !_context.Reservations.Any(reservation =>
                        reservation.SiteID == site.SiteID &&
                        reservation.ReservationStatus !=
                            ReservationStatus.Cancelled &&
                        reservation.CheckInDate < requestedCheckOut &&
                        reservation.CheckOutDate > requestedCheckIn));
            }

            var availableSites = await sitesQuery
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            return View(availableSites);
        }

        // ---------------------------------------------------------
        // ADMIN SITE MANAGEMENT
        // ---------------------------------------------------------

        // GET: Sites
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sites = await _context.Sites
                .Include(s => s.SiteType)
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            return View(sites);
        }

        // GET: Sites/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var site = await _context.Sites
                .Include(s => s.SiteType)
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.SiteID == id);

            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // GET: Sites/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(st => st.Name)
                .ToListAsync();

            return View();
        }

        // POST: Sites/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "SiteID,SiteNumber,SiteTypeID,SiteStatus," +
                "MaxRVLength,BaseRate")]
            Site site)
        {
            if (ModelState.IsValid)
            {
                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(st => st.Name)
                .ToListAsync();

            return View(site);
        }

        // GET: Sites/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound();
            }

            ViewBag.SiteTypes = await _context.SiteTypes
                .OrderBy(st => st.Name)
                .ToListAsync();

            return View(site);
        }

        // POST: Sites/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "SiteID,SiteNumber,SiteTypeID,SiteStatus," +
                "MaxRVLength,BaseRate")]
            Site site)
        {
            if (id != site.SiteID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SiteTypes = await _context.SiteTypes
                    .OrderBy(st => st.Name)
                    .ToListAsync();

                return View(site);
            }

            try
            {
                _context.Sites.Update(site);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SiteExists(site.SiteID))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Sites/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.SiteID == id);

            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // POST: Sites/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound();
            }

            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool SiteExists(int id)
        {
            return _context.Sites.Any(s =>
                s.SiteID == id);
        }
    }
}