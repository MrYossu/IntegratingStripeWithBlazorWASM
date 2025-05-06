namespace StripeInWasm2.Server.Services;

public class StripeOptions {
  public string SecretKey { get; set; } = ""; 
  public string PublishableKey { get; set; } = "";
  public string WebhookSecret { get; set; } = "";
}