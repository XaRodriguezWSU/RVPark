using Microsoft.AspNetCore.Mvc;
using RVSite.Models;
using Microsoft.EntityFrameworkCore;

namespace RVSite.Controllers
{
    public class SitePhotoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SitePhotoController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /SitePhoto/Manage
        public async Task<IActionResult> Manage()
        {
            var sites = await _context.Sites
                .Include(s => s.SiteType)
                .Include(s => s.Photos)
                .OrderBy(s => s.SiteNumber)
                .ToListAsync();

            return View(sites);
        }

        // GET: /SitePhoto/Index/5
        public async Task<IActionResult> Index(int id)
        {
            var site = await _context.Sites
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(s => s.SiteID == id);

            if (site == null)
                return NotFound();

            return View(site);
        }

        // GET: /SitePhoto/Create/5
        public IActionResult Create(int id)
        {
            return View(new SitePhoto { SiteID = id });
        }

        // POST: /SitePhoto/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int id, SitePhoto model, IFormFile file)
        {
            model.SiteID = id;

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please upload a photo.");
                return View(model);
            }

            // Create folder
            var folderPath = Path.Combine(_env.WebRootPath, "storage", "photos", model.SiteID.ToString());
            Directory.CreateDirectory(folderPath);

            // Save file
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            model.FilePath = $"/storage/photos/{model.SiteID}/{fileName}";
            model.UploadedAt = DateTime.Now;

            _context.SitePhoto.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = model.SiteID });
        }

        // GET: /SitePhoto/Edit/10
        public async Task<IActionResult> Edit(int id)
        {
            var photo = await _context.SitePhoto.FindAsync(id);
            if (photo == null)
                return NotFound();

            return View(photo);
        }

        // POST: /SitePhoto/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SitePhoto model)
        {
            _context.SitePhoto.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { id = model.SiteID });
        }

        // GET: /SitePhoto/Delete/10
        public async Task<IActionResult> Delete(int id)
        {
            var photo = await _context.SitePhoto.FindAsync(id);
            if (photo == null)
                return NotFound();

            return View(photo);
        }

        // POST: /SitePhoto/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var photo = await _context.SitePhoto.FindAsync(id);
            if (photo == null)
                return NotFound();

            // Delete file
            var fullPath = Path.Combine(_env.WebRootPath, photo.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.SitePhoto.Remove(photo);
            await _context.SaveChangesAsync();

            return View("DeleteConfirmed", photo);

        }
    }
}
