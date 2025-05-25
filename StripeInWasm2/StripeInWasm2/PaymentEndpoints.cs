using StripeInWasm2.Common.Models;
using StripeInWasm2.Server.Services;

namespace StripeInWasm2.Server;

public static class PaymentEndpoints {
  public static RouteGroupBuilder MapPaymentApi(this RouteGroupBuilder group) {
    group.MapGet("/config", (PaymentService paymentService) => 
      Results.Ok(new {
        PublishableKey = paymentService.GetPublishableKey()
      }));

    group.MapPost("/process-payment", async (PaymentService paymentService, ProcessPaymentRequest request) => {
      try {
        // Process the payment on the server using the payment method ID
        PaymentResult result = await paymentService.ProcessPayment(request);
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