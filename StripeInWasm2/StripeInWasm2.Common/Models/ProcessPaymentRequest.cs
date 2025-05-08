namespace StripeInWasm2.Common.Models;

/// <summary>
/// Holds the information needed to create a payment intent.
/// </summary>
public class ProcessPaymentRequest {
  public string PaymentMethodId { get; set; } = "";
  public string PaymentIntentId { get; set; } = "";
  public long Amount { get; set; }
  public string BaseUrl { get; set; } = "";
}