using StripeInWasm2.Common.Models;
using StripeInWasm2.Server.Services;
using PaymentIntentRequest = StripeInWasm2.Common.Models.PaymentIntentRequest;
using PaymentResult = StripeInWasm2.Common.Models.PaymentResult;

namespace StripeInWasm2.Server;

public static class PaymentEndpoints {
  public static RouteGroupBuilder MapPaymentApi(this RouteGroupBuilder group) {
    group.MapGet("/config", (PaymentService paymentService) => Results.Ok(new { PublishableKey = paymentService.GetPublishableKey() }));

    group.MapPost("/prepare-payment-intent", async (PaymentService paymentService, PaymentIntentRequest request) => {
      try {
        // Create payment intent on the server, but only return the ID, not the secret
        string paymentIntentId = await paymentService.CreatePaymentIntent(
          request.Amount,
          request.Currency.ToLower(),
          request.Description);

        return Results.Ok(new { PaymentIntentId = paymentIntentId });
      }
      catch (Exception ex) {
        return Results.BadRequest(new { Error = ex.Message });
      }
    });

    group.MapPost("/process-payment", async (PaymentService paymentService, ProcessPaymentRequest request) => {
      try {
        // Process the payment on the server using the payment method ID
        PaymentResult result = await paymentService.ProcessPayment(request.PaymentMethodId, request.Amount);

        return Results.Ok(result);
      }
      catch (Exception ex) {
        return Results.BadRequest(new {
          IsSuccessful = false,
          Status = "error",
          ErrorMessage = ex.Message
        });
      }
    });

    return group;
  }
}