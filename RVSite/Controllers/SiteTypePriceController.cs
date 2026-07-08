using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public IActionResult Index(int id)
        {
            var prices = _context.SiteTypePrices
                .Where(p => p.SiteTypeID == id)
                .Include(p => p.SiteType)
                .OrderBy(p => p.StartDate)
                .ToList();

            ViewBag.SiteType = _context.SiteTypes.Find(id);

            return View(prices);
        }

        public IActionResult Create(int id)
        {
            Console.WriteLine("DEBUG: Create GET received id = " + id);

            if (id == 0)
                return BadRequest("siteTypeId is required.");

            var model = new SiteTypePrice
            {
                SiteTypeID = id
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(SiteTypePrice model)
        {
            Console.WriteLine("DEBUG: Posted SiteTypeID = " + model.SiteTypeID);

            if (!ModelState.IsValid)
                return View(model);

            _context.SiteTypePrices.Add(model);
            _context.SaveChanges();
            return RedirectToAction("Index", new { id = model.SiteTypeID });
        }

        public IActionResult Edit(int id)
        {
            var price = _context.SiteTypePrices.Find(id);
            if (price == null) return NotFound();
            return View(price);
        }

        [HttpPost]
        public IActionResult Edit(SiteTypePrice model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _context.SiteTypePrices.Update(model);
            _context.SaveChanges();

            return RedirectToAction("Index", new { id = model.SiteTypeID });
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

            return RedirectToAction("Index", new { id = siteTypeId });
        }
    }
}
