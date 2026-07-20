using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVSite.Data;
using RVSite.Models;
using RVSite.Services;

namespace RVSite.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly StripeApiAdapter _stripeAdapter;
        private readonly StripePaymentStrategy _stripeStrategy;

        private IPaymentStrategy _strategy = null!;

        public PaymentController(
            AppDbContext context,
            EmailService emailService,
            StripeApiAdapter stripeAdapter,
            StripePaymentStrategy stripeStrategy
            )
        {
            _context = context;
            _emailService = emailService;
            _stripeAdapter = stripeAdapter;
            _stripeStrategy = stripeStrategy;
        }

        private void SetPaymentStrategy(PaymentMethodType method)
        {
            _strategy = method switch
            {
                PaymentMethodType.Stripe => _stripeStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(method), "Only Stripe payments are enabled right now.")
            };
        }


        [HttpGet]
        public async Task<IActionResult> Checkout(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null) return NotFound();

            ViewBag.AmountDue = reservation.BalanceDue > 0 ? reservation.BalanceDue : reservation.TotalCost;
            ViewBag.IsEmployee = User.IsInRole("Staff") || User.IsInRole("Admin");

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


        private async Task CompleteTransactionAsync(Reservation reservation, Payment payment)
        {
            reservation.ReservationStatus = ReservationStatus.Confirmed;
            reservation.BalanceDue = Math.Max(0, reservation.TotalCost - payment.AmountPaid);

            await _context.SaveChangesAsync();

            if (reservation.User?.Email != null)
            {
                var subject = $"Reservation #{reservation.ReservationID} Confirmed";
                var body = $"<p>Hi {reservation.User.FirstName},</p>" +
                           $"<p>Your reservation (#{reservation.ReservationID}) is confirmed.</p>" +
                           $"<p>Amount paid: {payment.AmountPaid:C}<br/>" +
                           $"Check-in: {reservation.CheckInDate:MMMM d, yyyy}<br/>" +
                           $"Check-out: {reservation.CheckOutDate:MMMM d, yyyy}</p>" +
                           $"<p>Thank you for booking with us!</p>";

                await _emailService.SendAsync(reservation.User.Email, subject, body);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                    .ThenInclude(s => s!.SiteType)
                .FirstOrDefaultAsync(r => r.ReservationID == reservationId);

            if (reservation == null) return NotFound();

            return View("~/Views/ClientReservation/Confirmation.cshtml", reservation);
        }
    }
}