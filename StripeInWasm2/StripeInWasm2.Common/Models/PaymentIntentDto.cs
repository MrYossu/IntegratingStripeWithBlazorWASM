namespace StripeInWasm2.Common.Models;

public class PaymentIntentDto {
  public string Status { get; set; } = "";
  public string Id { get; set; } = "";
  public int Amount { get; set; }
}