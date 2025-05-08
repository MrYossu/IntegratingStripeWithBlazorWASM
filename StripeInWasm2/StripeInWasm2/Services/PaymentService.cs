using System.Net;
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
      CaptureMethod = "automatic",
      PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions {
        Card = new PaymentIntentPaymentMethodOptionsCardOptions {
          RequestThreeDSecure = "any",
        }
      }
    };

    PaymentIntentService service = new();
    PaymentIntent paymentIntent = await service.CreateAsync(options);

    // Return only the ID, not the client secret
    return paymentIntent.Id;
  }

  public async Task<PaymentResult> ProcessPayment(ProcessPaymentRequest request) {
    if (string.IsNullOrEmpty(request.PaymentMethodId)) {
      throw new ArgumentException("Payment method ID is required");
    }

    try {
      PaymentIntentCreateOptions options = new() {
        Amount = request.Amount,
        Currency = "gbp",
        PaymentMethod = request.PaymentMethodId,
        Confirm = true,
        ConfirmationMethod = "automatic",
        ReturnUrl = $"{request.BaseUrl}post3d.html",
        PaymentMethodTypes = ["card"]
      };

      PaymentIntentService service = new();
      PaymentIntent paymentIntent = await service.CreateAsync(options);
      return paymentIntent.Status switch {
        "succeeded" => new PaymentResult {
          PaymentMethodId = paymentIntent.Id,
          Status = PaymentResultStatuses.Success
        },
        "requires_action" => new PaymentResult {
          PaymentMethodId = paymentIntent.Id,
          Status = PaymentResultStatuses.Redirect,
          Message = paymentIntent.NextAction.RedirectToUrl.Url,
          ReturnUri = options.ReturnUrl,
          ClientSecret = paymentIntent.ClientSecret // This is needed for 3D Secure
        },
        _ => new PaymentResult {
          PaymentMethodId = paymentIntent.Id,
          Status = PaymentResultStatuses.Declined,
          Message = $"Payment failed with status: {paymentIntent.Status}"
        }
      };
    }
    catch (StripeException e) {
      return new PaymentResult {
        // For some reason, the API throws an error if the card is declined, so we need to check the HTTP status code to determine if this was a genuine error, or just the card being declined
        Status = e.HttpStatusCode == HttpStatusCode.PaymentRequired 
          ? PaymentResultStatuses.Declined 
          : PaymentResultStatuses.Error,
        Message = e.Message
      };
    }
  }
}