using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Models;
using System;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    [Authorize(Roles = "Staff")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            var todaysArrivals = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckInDate.Date == today)
                .OrderBy(r => r.CheckInDate)
                .ToListAsync();

            var todaysDepartures = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckOutDate.Date == today)
                .OrderBy(r => r.CheckOutDate)
                .ToListAsync();

            var openMaintenanceTasks = await _context.MaintenanceTasks
                .Include(t => t.Site)
                .Where(t =>
                    t.Status == MaintenanceTaskStatus.Open ||
                    t.Status == MaintenanceTaskStatus.InProgress)
                .OrderByDescending(t =>
                    t.Priority == MaintenanceTaskPriority.Urgent)
                .ThenBy(t => t.CreatedAt)
                .Take(6)
                .ToListAsync();

            var unpaidReservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.BalanceDue > 0)
                .OrderBy(r => r.CheckInDate)
                .Take(6)
                .ToListAsync();

            ViewBag.TodayArrivalsCount = todaysArrivals.Count;
            ViewBag.TodayDeparturesCount = todaysDepartures.Count;

            ViewBag.OpenMaintenanceCount =
                await _context.MaintenanceTasks.CountAsync(t =>
                    t.Status == MaintenanceTaskStatus.Open ||
                    t.Status == MaintenanceTaskStatus.InProgress);

            ViewBag.OutstandingPaymentsCount =
                await _context.Reservations.CountAsync(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.BalanceDue > 0);

            ViewBag.TodaysArrivals = todaysArrivals;
            ViewBag.TodaysDepartures = todaysDepartures;
            ViewBag.OpenMaintenanceTasks = openMaintenanceTasks;
            ViewBag.UnpaidReservations = unpaidReservations;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Daily(DateTime? date)
        {
            var selectedDate = date?.Date ?? DateTime.Today;

            var arrivals = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckInDate.Date == selectedDate)
                .OrderBy(r => r.Site!.SiteNumber)
                .ToListAsync();

            var departures = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .Where(r =>
                    r.ReservationStatus != ReservationStatus.Cancelled &&
                    r.CheckOutDate.Date == selectedDate)
                .OrderBy(r => r.Site!.SiteNumber)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;
            ViewBag.Arrivals = arrivals;
            ViewBag.Departures = departures;

            return View();
        }
    }


    }