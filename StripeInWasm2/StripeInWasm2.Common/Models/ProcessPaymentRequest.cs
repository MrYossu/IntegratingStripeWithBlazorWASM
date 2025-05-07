namespace StripeInWasm2.Common.Models;

public class ProcessPaymentRequest {
  public string PaymentMethodId { get; set; } = "";
  public string PaymentIntentId { get; set; } = "";
  public long Amount { get; set; }
  public string BaseUrl { get; set; } = "";
}