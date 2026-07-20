using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace RVSite.Services
{
    public class StripeSessionResult
    {
        public string SessionId { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class StripeApiAdapter
    {
        private readonly string _webhookSecret;

        public StripeApiAdapter(IConfiguration config)
        {
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
            _webhookSecret = config["Stripe:WebhookSecret"] ?? "";
        }

        public async Task<StripeSessionResult> CreateSessionAsync(decimal amount, string reservationReference, string successUrl, string cancelUrl)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amount * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Reservation #{reservationReference}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = reservationReference
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return new StripeSessionResult { SessionId = session.Id, Url = session.Url };
        }

        public bool TryVerifyWebhook(string json, string signatureHeader, out Event? stripeEvent)
        {
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _webhookSecret);
                return true;
            }
            catch (StripeException)
            {
                stripeEvent = null;
                return false;
            }
        }
    }
}