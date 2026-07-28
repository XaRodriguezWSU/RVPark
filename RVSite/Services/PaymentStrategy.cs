using Microsoft.Extensions.Configuration;
using RVSite.Models;

namespace RVSite.Services
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Error { get; set; }
    }

    public interface IPaymentStrategy
    {
        // employeeUserId is null for Stripe (customer-initiated)
        Task<PaymentResult> ProcessAsync(Reservation reservation, Payment payment, int? employeeUserId = null);
    }

    public class StripePaymentStrategy : IPaymentStrategy
    {
        private readonly StripeApiAdapter _stripe;
        private readonly string _baseUrl;

        public StripePaymentStrategy(StripeApiAdapter stripe, IConfiguration config)
        {
            _stripe = stripe;
            _baseUrl = (config["App:BaseUrl"] ?? "").TrimEnd('/');
        }

        public async Task<PaymentResult> ProcessAsync(Reservation reservation, Payment payment, int? employeeUserId = null)
        {
            payment.ReservationID = reservation.ReservationID;
            payment.Method = PaymentMethodType.Stripe;
            payment.Status = PaymentStatus.Pending;

            var successUrl = $"{_baseUrl}/Payment/Confirmation?reservationId={reservation.ReservationID}&session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{_baseUrl}/Payment/Checkout?reservationId={reservation.ReservationID}";

            var session = await _stripe.CreateSessionAsync(payment.AmountPaid, reservation.ReservationID.ToString(), successUrl, cancelUrl);
            payment.TransactionReference = session.SessionId;

            return new PaymentResult { Success = true, RedirectUrl = session.Url };
        }
    }

    public abstract class OfflinePaymentStrategy : IPaymentStrategy
    {
        protected abstract PaymentMethodType Method { get; }
 
        public Task<PaymentResult> ProcessAsync(Reservation reservation, Payment payment, int? employeeUserId = null)
        {
            if (employeeUserId == null)
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    Error = $"{Method} payments must be recorded by an employee."
                });
            }
 
            payment.ReservationID = reservation.ReservationID;
            payment.Method = Method;
            payment.Status = PaymentStatus.Paid;
            payment.ProcessedByUserID = employeeUserId;
 
            return Task.FromResult(new PaymentResult { Success = true });
        }
    }
 
    public class CashPaymentStrategy : OfflinePaymentStrategy
    {
        protected override PaymentMethodType Method => PaymentMethodType.Cash;
    }
 
    public class CheckPaymentStrategy : OfflinePaymentStrategy
    {
        protected override PaymentMethodType Method => PaymentMethodType.Check;
    }
 
    public class ManualCardPaymentStrategy : OfflinePaymentStrategy
    {
        protected override PaymentMethodType Method => PaymentMethodType.ManualCard;
    }
  
}