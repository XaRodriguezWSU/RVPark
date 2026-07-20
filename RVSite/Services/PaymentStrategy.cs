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

            var successUrl = $"{_baseUrl}/Payment/Confirmation?reservationId={reservation.ReservationID}";
            var cancelUrl = $"{_baseUrl}/Payment/Checkout?reservationId={reservation.ReservationID}";

            var session = await _stripe.CreateSessionAsync(payment.AmountPaid, reservation.ReservationID.ToString(), successUrl, cancelUrl);
            payment.TransactionReference = session.SessionId;

            return new PaymentResult { Success = true, RedirectUrl = session.Url };
        }
    }

}