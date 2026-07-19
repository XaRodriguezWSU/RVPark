using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RVSite.Models;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SiteTypeController : Controller
    {
        private readonly AppDbContext _context;

        public SiteTypeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.SiteTypes.ToList());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(SiteType type)
        {
            if (!ModelState.IsValid)
                return View(type);

            _context.SiteTypes.Add(type);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var type = _context.SiteTypes.Find(id);
            if (type == null) return NotFound();
            return View(type);
        }

        [HttpPost]
        public IActionResult Edit(SiteType type)
        {
            if (!ModelState.IsValid)
                return View(type);

            _context.SiteTypes.Update(type);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var type = _context.SiteTypes.Find(id);
            if (type == null) return NotFound();
            return View(type);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var type = _context.SiteTypes.Find(id);
            _context.SiteTypes.Remove(type);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
