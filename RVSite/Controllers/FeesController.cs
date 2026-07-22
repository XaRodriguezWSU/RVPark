using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using RVSite.Services;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly CostService _costService;

        public FeesController(AppDbContext context, CostService costService)
        {
            _context = context;
            _costService = costService;
        }

        // GET: Fees & send to fee index view
        public async Task<IActionResult> Index()
        {
            return View(await _context.Fees.ToListAsync());
        }

        // GET: Open one fee & it's details based on feeid
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fee = await _context.Fees
                .FirstOrDefaultAsync(m => m.FeeID == id);

            if (fee == null)
            {
                return NotFound();
            }

            return View(fee);
        }

        // GET: Display "create fee" view
        public IActionResult Create()
        {
            LoadReservationDropDown();
            return View();
        }

        // POST: Save the newly created (valid) fee & return user to "list of fees" view
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FeeID,ReservationID,NameCode,Amount,EffectiveDate")] Fee fee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fee);
                await _context.SaveChangesAsync();

                await _costService.UpdateReservationBalanceDueAsync(fee.ReservationID);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            LoadReservationDropDown(fee.ReservationID);
            return View(fee);
        }

        // GET: Open fee edit page based on feeid
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fee = await _context.Fees.FindAsync(id);

            if (fee == null)
            {
                return NotFound();
            }

            LoadReservationDropDown(fee.ReservationID);
            return View(fee);
        }

        // POST: Save fee edit changes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FeeID,ReservationID,NameCode,Amount,EffectiveDate")] Fee fee)
        {
            if (id != fee.FeeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingFee = await _context.Fees
                   .AsNoTracking()
                   .FirstOrDefaultAsync(f => f.FeeID == id);

                if (existingFee == null)
                {
                    return NotFound();
                }

                int oldReservationID = existingFee.ReservationID;

                try
                {
                    _context.Update(fee);
                    await _context.SaveChangesAsync();

                    await _costService.UpdateReservationBalanceDueAsync(oldReservationID);

                    if (oldReservationID != fee.ReservationID)
                    {
                        await _costService.UpdateReservationBalanceDueAsync(fee.ReservationID);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FeeExists(fee.FeeID))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            LoadReservationDropDown(fee.ReservationID);
            return View(fee);
        }

        // GET: Display deletion confirmation page
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var fee = await _context.Fees
                .FirstOrDefaultAsync(m => m.FeeID == id);

            if (fee == null)
            {
                return NotFound();
            }

            return View(fee);
        }

        // POST: Delete fee after confirmation is recieved
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fee = await _context.Fees.FindAsync(id);

            if (fee != null)
            {
                int reservationID = fee.ReservationID;

                _context.Fees.Remove(fee);
                await _context.SaveChangesAsync();

                await _costService.UpdateReservationBalanceDueAsync(reservationID);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper: Check that fee exists in db
        private bool FeeExists(int id)
        {
            return _context.Fees.Any(e => e.FeeID == id);
        }

        // Helper: Reservation selector for create and edit
        private void LoadReservationDropDown(int? selectedReservationID = null)
        {
            var reservations = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Select(r => new
                {
                    r.ReservationID,
                    DisplayText = "Reservation #" + r.ReservationID
                        + " - " + r.User.FirstName + " " + r.User.LastName
                        + " - Site " + r.Site.SiteNumber
                })
                .ToList();

            ViewBag.ReservationID = new SelectList(reservations, "ReservationID", "DisplayText", selectedReservationID);
        }
    }
}
