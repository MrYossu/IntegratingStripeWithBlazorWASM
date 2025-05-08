namespace StripeInWasm2.Client.Models;

/// <summary>
/// DTO to pass information about the result of a 3D authorisation request from the JavaScript to the Checkout component. This copies the properties of interest from the PaymentIntent type in the Stripe.Net package. We don't use that type, as the package is not referenced in the WASM project, and there's no reason to reference when we would only need it here.
/// </summary>
public class PaymentIntentDto {
  public string Status { get; set; } = "";
  public string Id { get; set; } = "";
  public int Amount { get; set; }
}