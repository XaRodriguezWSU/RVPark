using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SiteTypePriceController : Controller
    {
        private readonly AppDbContext _context;

        public SiteTypePriceController(AppDbContext context)
        {
            _context = context;
        }

        // GET: SiteTypePrice?siteTypeId=1
        [HttpGet]
        public IActionResult Index(int siteTypeId)
        {
            if (siteTypeId <= 0)
            {
                return BadRequest("A site type is required.");
            }

            var siteType = _context.SiteTypes
                .FirstOrDefault(st => st.SiteTypeID == siteTypeId);

            if (siteType == null)
            {
                return NotFound();
            }

            var prices = _context.SiteTypePrices
                .Where(p => p.SiteTypeID == siteTypeId)
                .Include(p => p.SiteType)
                .OrderBy(p => p.StartDate)
                .ToList();

            ViewBag.SiteType = siteType;
            ViewBag.SiteTypeID = siteType.SiteTypeID;
            ViewBag.SiteTypeName = siteType.Name;

            return View(prices);
        }

        // GET: SiteTypePrice/Create?siteTypeId=1
        [HttpGet]
        public IActionResult Create(int siteTypeId)
        {
            if (siteTypeId <= 0)
            {
                return BadRequest("A site type is required.");
            }

            var siteType = _context.SiteTypes
                .FirstOrDefault(st => st.SiteTypeID == siteTypeId);

            if (siteType == null)
            {
                return NotFound();
            }

            var model = new SiteTypePrice
            {
                SiteTypeID = siteTypeId,
                StartDate = DateTime.Today
            };

            ViewBag.SiteType = siteType;
            ViewBag.SiteTypeName = siteType.Name;

            return View(model);
        }

        // POST: SiteTypePrice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SiteTypePrice model)
        {
            var siteType = _context.SiteTypes
                .FirstOrDefault(st => st.SiteTypeID == model.SiteTypeID);

            if (siteType == null)
            {
                ModelState.AddModelError(
                    nameof(model.SiteTypeID),
                    "The selected site type does not exist.");
            }

            if (model.EndDate.HasValue &&
                model.EndDate.Value.Date < model.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "The end date cannot be before the start date.");
            }

            if (model.Price < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Price),
                    "The nightly rate cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SiteType = siteType;
                ViewBag.SiteTypeName = siteType?.Name;

                return View(model);
            }

            _context.SiteTypePrices.Add(model);
            _context.SaveChanges();

            return RedirectToAction(
                nameof(Index),
                new { siteTypeId = model.SiteTypeID });
        }

        // GET: SiteTypePrice/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var price = _context.SiteTypePrices
                .Include(p => p.SiteType)
                .FirstOrDefault(p => p.SiteTypePriceID == id);

            if (price == null)
            {
                return NotFound();
            }

            ViewBag.SiteType = price.SiteType;
            ViewBag.SiteTypeName = price.SiteType?.Name;

            return View(price);
        }

        // POST: SiteTypePrice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, SiteTypePrice model)
        {
            if (id != model.SiteTypePriceID)
            {
                return NotFound();
            }

            var siteType = _context.SiteTypes
                .FirstOrDefault(st => st.SiteTypeID == model.SiteTypeID);

            if (siteType == null)
            {
                ModelState.AddModelError(
                    nameof(model.SiteTypeID),
                    "The selected site type does not exist.");
            }

            if (model.EndDate.HasValue &&
                model.EndDate.Value.Date < model.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "The end date cannot be before the start date.");
            }

            if (model.Price < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Price),
                    "The nightly rate cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.SiteType = siteType;
                ViewBag.SiteTypeName = siteType?.Name;

                return View(model);
            }

            try
            {
                _context.SiteTypePrices.Update(model);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = _context.SiteTypePrices
                    .Any(p => p.SiteTypePriceID == model.SiteTypePriceID);

                if (!exists)
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(
                nameof(Index),
                new { siteTypeId = model.SiteTypeID });
        }

        // GET: SiteTypePrice/Delete/5
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var price = _context.SiteTypePrices
                .Include(p => p.SiteType)
                .FirstOrDefault(p => p.SiteTypePriceID == id);

            if (price == null)
            {
                return NotFound();
            }

            return View(price);
        }

        // POST: SiteTypePrice/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var price = _context.SiteTypePrices
                .FirstOrDefault(p => p.SiteTypePriceID == id);

            if (price == null)
            {
                return NotFound();
            }

            var siteTypeId = price.SiteTypeID;

            _context.SiteTypePrices.Remove(price);
            _context.SaveChanges();

            return RedirectToAction(
                nameof(Index),
                new { siteTypeId });
        }
    }
}