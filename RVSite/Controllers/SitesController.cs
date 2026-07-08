
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;

public class SitesController : Controller
{
    private readonly AppDbContext _context;

    public SitesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: SITES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Sites.Include(s => s.SiteType).ToListAsync());
    }

    // GET: SITES/Details/5
    public async Task<IActionResult> Details(int id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var site = await _context.Sites
            .Include(s => s.SiteType)
            .FirstOrDefaultAsync(m => m.SiteID == id);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    // GET: SITES/Create
    public IActionResult Create()
    {
        ViewBag.SiteTypes = _context.SiteTypes.ToList();
        return View();
    }

    // POST: SITES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SiteID,SiteNumber,SiteTypeID,SiteStatus,MaxRVLength,BaseRate")] Site site)
    {
        if (ModelState.IsValid)
        {
            _context.Add(site);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.SiteTypes = _context.SiteTypes.ToList();
        return View(site);
    }

    // GET: SITES/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var site = await _context.Sites.FindAsync(id);
        if (site == null)
        {
            return NotFound();
        }
        ViewBag.SiteTypes = _context.SiteTypes.ToList();
        return View(site);
    }

    // POST: SITES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("SiteID,SiteNumber,SiteTypeID,SiteStatus,MaxRVLength,BaseRate")] Site site)
    {
        if (id != site.SiteID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(site);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SiteExists(site.SiteID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(site);
    }

    // GET: SITES/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var site = await _context.Sites
            .FirstOrDefaultAsync(m => m.SiteID == id);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    // POST: SITES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var site = await _context.Sites.FindAsync(id);
        if (site != null)
        {
            _context.Sites.Remove(site);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SiteExists(int id)
    {
        return _context.Sites.Any(e => e.SiteID == id);
    }
}
