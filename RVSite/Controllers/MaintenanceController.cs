using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MaintenanceController : Controller
    {
        private readonly AppDbContext _context;

        public MaintenanceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<MaintenanceTask> tasks =
                await GetOpenTasksAsync();

            await LoadPageDataAsync(tasks);

            return View(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            MaintenanceTask maintenanceTask)
        {
            int? currentUserID = GetCurrentUserID();

            if (currentUserID == null)
            {
                return Challenge();
            }

            maintenanceTask.CreatedByUserID =
                currentUserID.Value;

            maintenanceTask.Status =
                MaintenanceTaskStatus.Open;

            maintenanceTask.CreatedAt =
                DateTime.Now;

            maintenanceTask.CompletedAt = null;
            maintenanceTask.ClosedByUserID = null;

            // Navigation properties are loaded by EF later.
            ModelState.Remove(nameof(MaintenanceTask.Site));
            ModelState.Remove(nameof(MaintenanceTask.CreatedByUser));
            ModelState.Remove(nameof(MaintenanceTask.ClosedByUser));

            if (!ModelState.IsValid)
            {
                List<MaintenanceTask> tasks =
                    await GetOpenTasksAsync();

                await LoadPageDataAsync(tasks);

                ViewBag.NewTask = maintenanceTask;

                return View("Index", tasks);
            }

            _context.MaintenanceTasks.Add(maintenanceTask);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The maintenance ticket was submitted.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            MaintenanceTask? task =
                await _context.MaintenanceTasks
                    .FirstOrDefaultAsync(t =>
                        t.MaintenanceTaskID == id);

            if (task == null)
            {
                return NotFound();
            }

            if (task.Status != MaintenanceTaskStatus.Open)
            {
                TempData["ErrorMessage"] =
                    "Only open tasks can be started.";

                return RedirectToAction(nameof(Index));
            }

            task.Status =
                MaintenanceTaskStatus.InProgress;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The maintenance task is now in progress.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            MaintenanceTask? task =
                await _context.MaintenanceTasks
                    .FirstOrDefaultAsync(t =>
                        t.MaintenanceTaskID == id);

            if (task == null)
            {
                return NotFound();
            }

            int? currentUserID = GetCurrentUserID();

            if (currentUserID == null)
            {
                return Challenge();
            }

            if (task.Status == MaintenanceTaskStatus.Completed)
            {
                TempData["ErrorMessage"] =
                    "This task has already been completed.";

                return RedirectToAction(nameof(Index));
            }

            task.Status =
                MaintenanceTaskStatus.Completed;

            task.CompletedAt =
                DateTime.Now;

            task.ClosedByUserID =
                currentUserID.Value;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "The maintenance task was completed.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<MaintenanceTask>>
            GetOpenTasksAsync()
        {
            return await _context.MaintenanceTasks
                .Include(t => t.Site)
                .Include(t => t.CreatedByUser)
                .Where(t =>
                    t.Status == MaintenanceTaskStatus.Open ||
                    t.Status == MaintenanceTaskStatus.InProgress)
                .OrderByDescending(t =>
                    t.Priority == MaintenanceTaskPriority.Urgent)
                .ThenByDescending(t =>
                    t.Priority == MaintenanceTaskPriority.Regular)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        private async Task LoadPageDataAsync(
            List<MaintenanceTask> tasks)
        {
            ViewBag.Sites =
                await _context.Sites
                    .OrderBy(s => s.SiteNumber)
                    .ToListAsync();

            ViewBag.UrgentCount =
                tasks.Count(t =>
                    t.Priority ==
                    MaintenanceTaskPriority.Urgent);

            ViewBag.RegularCount =
                tasks.Count(t =>
                    t.Priority ==
                    MaintenanceTaskPriority.Regular);

            ViewBag.LowCount =
                tasks.Count(t =>
                    t.Priority ==
                    MaintenanceTaskPriority.Low);
        }

        private int? GetCurrentUserID()
        {
            string? userIDValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (int.TryParse(
                userIDValue,
                out int userID))
            {
                return userID;
            }

            return null;
        }
    }
}