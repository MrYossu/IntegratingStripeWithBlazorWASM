namespace StripeInWasm2.Common.Models;

public class PaymentResult {
  public bool Success { get; set; }
  public string PaymentMethodId { get; set; } = "";
  public string Status { get; set; } = "";
  public string ErrorMessage { get; set; } = "";
  public string ClientSecret { get; set; } = "";
}