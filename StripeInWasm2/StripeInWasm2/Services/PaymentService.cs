using Microsoft.Extensions.Options;
using Stripe;
using StripeInWasm2.Common.Models;

namespace StripeInWasm2.Server.Services;

public class PaymentService {
  private readonly StripeOptions _stripeOptions;

  public PaymentService(IOptions<StripeOptions> stripeOptions) {
    _stripeOptions = stripeOptions.Value;
    StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
  }

  public string GetPublishableKey() =>
    _stripeOptions.PublishableKey;

  public async Task<string> CreatePaymentIntent(long amount, string currency, string description) {
    PaymentIntentCreateOptions options = new() {
      Amount = amount,
      Currency = currency,
      Description = description,
      SetupFutureUsage = "off_session",
      CaptureMethod = "automatic"
    };

    PaymentIntentService service = new();
    PaymentIntent paymentIntent = await service.CreateAsync(options);

    // Return only the ID, not the client secret
    return paymentIntent.Id;
  }

  public async Task<PaymentResult> ProcessPayment(string paymentMethodId, long amount) {
    if (string.IsNullOrEmpty(paymentMethodId)) {
      throw new ArgumentException("Payment method ID is required");
    }

    try {
      PaymentIntentCreateOptions options = new() {
        Amount = amount,
        Currency = "gbp",
        PaymentMethod = paymentMethodId,
        Confirm = true,
        ConfirmationMethod = "automatic",
        ReturnUrl = "https://www.pixata.co.uk"
      };

      PaymentIntentService service = new();
      PaymentIntent paymentIntent = await service.CreateAsync(options);

      return paymentIntent.Status switch {
        "succeeded" => new PaymentResult {
          Success = true,
          PaymentMethodId = paymentIntent.Id,
          Status = paymentIntent.Status
        },
        "requires_action" => new PaymentResult {
          Success = false,
          PaymentMethodId = paymentIntent.Id,
          Status = paymentIntent.Status,
          ErrorMessage = $"This payment requires additional authentication steps. NextAction: {paymentIntent.NextAction}",
          ClientSecret = paymentIntent.ClientSecret // This is needed for 3D Secure
        },
        _ => new PaymentResult {
          Success = false,
          PaymentMethodId = paymentIntent.Id,
          Status = paymentIntent.Status,
          ErrorMessage = $"Payment failed with status: {paymentIntent.Status}"
        }
      };
    }
    catch (StripeException e) {
      return new PaymentResult {
        Success = false,
        Status = "error",
        ErrorMessage = e.Message
      };
    }
  }
}