using Microsoft.AspNetCore.Mvc;
using RVSite.Models;

namespace RVSite.Controllers
{
    public class SiteTypePriceController : Controller
    {
        private readonly AppDbContext _context;

        public SiteTypePriceController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int siteTypeId)
        {
            var prices = _context.SiteTypePrices
                .Where(p => p.SiteTypeID == siteTypeId)
                .OrderBy(p => p.StartDate)
                .ToList();

            ViewBag.SiteType = _context.SiteTypes.Find(siteTypeId);

            return View(prices);
        }

        public IActionResult Create(int siteTypeId)
        {
            ViewBag.SiteTypeID = siteTypeId;
            return View();
        }

        [HttpPost]
        public IActionResult Create(SiteTypePrice price)
        {
            if (!ModelState.IsValid)
                return View(price);

            _context.SiteTypePrices.Add(price);
            _context.SaveChanges();
            return RedirectToAction("Index", new { siteTypeId = price.SiteTypeID });
        }

        public IActionResult Edit(int id)
        {
            var price = _context.SiteTypePrices.Find(id);
            if (price == null) return NotFound();
            return View(price);
        }

        [HttpPost]
        public IActionResult Edit(SiteTypePrice price)
        {
            if (!ModelState.IsValid)
                return View(price);

            _context.SiteTypePrices.Update(price);
            _context.SaveChanges();

            return RedirectToAction("Index", new { siteTypeId = price.SiteTypeID });
        }

        public IActionResult Delete(int id)
        {
            var price = _context.SiteTypePrices.Find(id);
            if (price == null) return NotFound();
            return View(price);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var price = _context.SiteTypePrices.Find(id);
            int siteTypeId = price.SiteTypeID;

            _context.SiteTypePrices.Remove(price);
            _context.SaveChanges();

            return RedirectToAction("Index", new { siteTypeId });
        }
    }
}
