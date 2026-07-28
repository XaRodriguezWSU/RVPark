using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using RVSite.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RVSite.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly StripeApiAdapter _stripeAdapter;
        private readonly StripePaymentStrategy _stripeStrategy;
        private readonly CashPaymentStrategy _cashStrategy;
        private readonly CheckPaymentStrategy _checkStrategy;
        private readonly ManualCardPaymentStrategy _manualCardStrategy;

        private IPaymentStrategy _strategy = null!;

        public PaymentController(
            AppDbContext context,
            EmailService emailService,
            StripeApiAdapter stripeAdapter,
            StripePaymentStrategy stripeStrategy,
            CashPaymentStrategy cashStrategy,
            CheckPaymentStrategy checkStrategy,
            ManualCardPaymentStrategy manualCardStrategy
            )
        {
            _context = context;
            _emailService = emailService;
            _stripeAdapter = stripeAdapter;
            _stripeStrategy = stripeStrategy;
            _cashStrategy = cashStrategy;
            _checkStrategy = checkStrategy;
            _manualCardStrategy = manualCardStrategy;
        }

        private void SetPaymentStrategy(PaymentMethodType method)
        {
            _strategy = method switch
            {
                PaymentMethodType.Stripe => _stripeStrategy,
                PaymentMethodType.Cash => _cashStrategy,
                PaymentMethodType.Check => _checkStrategy,
                PaymentMethodType.ManualCard => _manualCardStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(method))
            };
        }


        [HttpGet]
        public async Task<IActionResult> Checkout(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            bool isEmployee =
                User.IsInRole("Staff") ||
                User.IsInRole("Admin");

            if (!isEmployee)
            {
                string? userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdClaim, out int userId) ||
                    reservation.UserID != userId)
                {
                    return Forbid();
                }
            }

            ViewBag.AmountDue = reservation.BalanceDue;
            ViewBag.IsEmployee = isEmployee;

            ViewBag.Payments = await _context.Payments
                .Where(p =>
                    p.ReservationID == reservation.ReservationID)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(reservation);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessStripePayment(int reservationId)
        {
            var reservation = await _context.Reservations.FindAsync(reservationId);
            if (reservation == null) return NotFound();

            var payment = new Payment
            {
                AmountPaid = reservation.BalanceDue > 0 ? reservation.BalanceDue : reservation.TotalCost,
                PaymentDate = DateTime.Now
            };

            SetPaymentStrategy(PaymentMethodType.Stripe);
            var result = await _strategy.ProcessAsync(reservation, payment);

            if (!result.Success)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Checkout), new { reservationId });
            }

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Redirect(result.RedirectUrl!);
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            string json;
            using (var reader = new StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (!_stripeAdapter.TryVerifyWebhook(json, signature, out var stripeEvent) || stripeEvent == null)
            {
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed" &&
                stripeEvent.Data.Object is Stripe.Checkout.Session session)
            {
                var payment = await _context.Payments
                    .Include(p => p.Reservation)
                        .ThenInclude(r => r!.User)
                    .FirstOrDefaultAsync(p => p.TransactionReference == session.Id);

                if (payment == null || payment.Reservation == null) return NotFound();

                if (payment.Status != PaymentStatus.Paid)
                {
                    payment.Status = PaymentStatus.Paid;
                    payment.PaymentDate = DateTime.Now;
                    await CompleteTransactionAsync(payment.Reservation, payment);
                }
            }

            return Ok();
        }


        private async Task CompleteTransactionAsync(
            Reservation reservation,
            Payment payment)
        {
            reservation.BalanceDue = Math.Max(
                0,
                reservation.BalanceDue - payment.AmountPaid);

            if (reservation.BalanceDue <= 0)
            {
                reservation.BalanceDue = 0;
                reservation.ReservationStatus =
                    ReservationStatus.Confirmed;
            }
            else
            {
                reservation.ReservationStatus =
                    ReservationStatus.Pending;
            }

            await _context.SaveChangesAsync();

            if (reservation.User?.Email != null)
            {
                string subject;
                string body;

                if (reservation.BalanceDue <= 0)
                {
                    subject =
                        $"Reservation #{reservation.ReservationID} Confirmed";

                    body =
                        $"<p>Hi {reservation.User.FirstName},</p>" +
                        $"<p>Your reservation " +
                        $"(#{reservation.ReservationID}) is confirmed.</p>" +
                        $"<p>Amount paid: {payment.AmountPaid:C}<br/>" +
                        $"Remaining balance: {reservation.BalanceDue:C}<br/>" +
                        $"Check-in: {reservation.CheckInDate:MMMM d, yyyy}<br/>" +
                        $"Check-out: {reservation.CheckOutDate:MMMM d, yyyy}</p>" +
                        $"<p>Thank you for booking with us!</p>";
                }
                else
                {
                    subject =
                        $"Payment Received for Reservation #{reservation.ReservationID}";

                    body =
                        $"<p>Hi {reservation.User.FirstName},</p>" +
                        $"<p>We received a payment for reservation " +
                        $"#{reservation.ReservationID}.</p>" +
                        $"<p>Amount paid: {payment.AmountPaid:C}<br/>" +
                        $"Remaining balance: {reservation.BalanceDue:C}</p>" +
                        $"<p>Your reservation will be confirmed when the " +
                        $"remaining balance is paid.</p>";
                }

                await _emailService.SendAsync(
                    reservation.User.Email,
                    subject,
                    body);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int reservationId, string? session_id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null) return NotFound();

            if (!string.IsNullOrEmpty(session_id))
            {
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionReference == session_id);

                if (payment != null && payment.Status != PaymentStatus.Paid)
                {
                    var isPaid = await _stripeAdapter.IsSessionPaidAsync(session_id);
                    if (isPaid)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaymentDate = DateTime.Now;
                        await CompleteTransactionAsync(reservation, payment);
                    }
                }
            }

            return View("~/Views/ClientReservation/Confirmation.cshtml", reservation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> RecordOfflinePayment(
            int reservationId,
            PaymentMethodType method,
            decimal amount,
            string? reference)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.ReservationStatus ==
                ReservationStatus.Cancelled)
            {
                TempData["Error"] =
                    "Payments cannot be recorded for a cancelled reservation.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            if (reservation.BalanceDue <= 0)
            {
                TempData["Error"] =
                    "This reservation has already been paid in full.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            if (amount <= 0)
            {
                TempData["Error"] =
                    "The payment amount must be greater than zero.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            if (amount > reservation.BalanceDue)
            {
                TempData["Error"] =
                    $"The payment cannot exceed the remaining balance " +
                    $"of {reservation.BalanceDue:C}.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            if (method == PaymentMethodType.Stripe)
            {
                TempData["Error"] =
                    "Use the online card payment option for Stripe payments.";

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            string? employeeIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (employeeIdClaim == null ||
                !int.TryParse(employeeIdClaim, out int employeeId))
            {
                return Forbid();
            }

            var payment = new Payment
            {
                ReservationID = reservation.ReservationID,
                AmountPaid = amount,
                PaymentDate = DateTime.Now,
                TransactionReference =
                    string.IsNullOrWhiteSpace(reference)
                        ? null
                        : reference.Trim()
            };

            SetPaymentStrategy(method);

            var result = await _strategy.ProcessAsync(
                reservation,
                payment,
                employeeId);

            if (!result.Success)
            {
                TempData["Error"] = result.Error;

                return RedirectToAction(
                    nameof(Checkout),
                    new { reservationId });
            }

            _context.Payments.Add(payment);

            await CompleteTransactionAsync(
                reservation,
                payment);

            TempData["SuccessMessage"] =
                reservation.BalanceDue <= 0
                    ? "Payment recorded. The reservation is now confirmed."
                    : $"Payment recorded. Remaining balance: " +
                      $"{reservation.BalanceDue:C}.";

            return RedirectToAction(
                nameof(Checkout),
                new { reservationId });
        }
    }
}