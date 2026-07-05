
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
        return View(await _context.Sites.ToListAsync());
    }

    // GET: SITES/Details/5
    public async Task<IActionResult> Details(int? siteid)
    {
        if (siteid == null)
        {
            return NotFound();
        }

        var site = await _context.Sites
            .FirstOrDefaultAsync(m => m.SiteID == siteid);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    // GET: SITES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: SITES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SiteID,SiteNumber,SiteType,SiteStatus,MaxRVLength,BaseRate")] Site site)
    {
        if (ModelState.IsValid)
        {
            _context.Add(site);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(site);
    }

    // GET: SITES/Edit/5
    public async Task<IActionResult> Edit(int? siteid)
    {
        if (siteid == null)
        {
            return NotFound();
        }

        var site = await _context.Sites.FindAsync(siteid);
        if (site == null)
        {
            return NotFound();
        }
        return View(site);
    }

    // POST: SITES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? siteid, [Bind("SiteID,SiteNumber,SiteType,SiteStatus,MaxRVLength,BaseRate")] Site site)
    {
        if (siteid != site.SiteID)
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
    public async Task<IActionResult> Delete(int? siteid)
    {
        if (siteid == null)
        {
            return NotFound();
        }

        var site = await _context.Sites
            .FirstOrDefaultAsync(m => m.SiteID == siteid);
        if (site == null)
        {
            return NotFound();
        }

        return View(site);
    }

    // POST: SITES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? siteid)
    {
        var site = await _context.Sites.FindAsync(siteid);
        if (site != null)
        {
            _context.Sites.Remove(site);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool SiteExists(int? siteid)
    {
        return _context.Sites.Any(e => e.SiteID == siteid);
    }
}
