namespace StripeInWasm2.Common.Models;

/// <summary>
/// Used to pass the information needed to create a payment intent from the WASM to the server. The payment service used the properties here to create a PaymentIntentCreateOptions, which is passed to Stripe, in return for a payment intent.
/// </summary>
public class PaymentIntentRequest {
  public long Amount { get; set; }
  public string Currency { get; set; } = "gbp";
  public string Description { get; set; } = "";
}