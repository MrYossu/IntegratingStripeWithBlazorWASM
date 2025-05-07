namespace StripeInWasm2.Common.Models;

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