namespace StripeInWasm2.Common.Models;

/// <summary>
/// Returned by our Stripe service after it calls the Stripe API createPaymentMethod. Confusingly, it is then reused by our payment service to return the result of the payment intent creation
/// </summary>
public class PaymentResult {
  public PaymentResultStatuses Status { get; set; }
  public string PaymentMethodId { get; set; } = "";
  public string Message { get; set; } = "";
  public string ReturnUri { get; set; } = "";
  public string ClientSecret { get; set; } = "";
}

public enum PaymentResultStatuses {
  Success,
  Declined,
  Redirect,
  Error
}