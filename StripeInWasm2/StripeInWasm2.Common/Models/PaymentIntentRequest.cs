namespace StripeInWasm2.Common.Models;

public class PaymentIntentRequest {
  public long Amount { get; set; }
  public string Currency { get; set; } = "gbp";
  public string Description { get; set; } = "";
}