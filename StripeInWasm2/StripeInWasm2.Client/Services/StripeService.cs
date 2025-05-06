using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using StripeInWasm2.Common.Models;

namespace StripeInWasm2.Client.Services;

public class StripeService : IAsyncDisposable {
  private readonly HttpClient _httpClient;
  private readonly IJSRuntime _jsRuntime;
  private IJSObjectReference? _module;
  private readonly DotNetObjectReference<StripeService> _dotNetRef;

  public StripeService(HttpClient httpClient, IJSRuntime jsRuntime) {
    _httpClient = httpClient;
    _jsRuntime = jsRuntime;
    _dotNetRef = DotNetObjectReference.Create(this);
  }

  public async Task InitializeStripe() {
    _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/stripeHelper.js");

    // Get the publishable key from backend
    StripeConfig? configResponse = await _httpClient.GetFromJsonAsync<StripeConfig>("/api/payment/config");

    if (configResponse != null && !string.IsNullOrEmpty(configResponse.PublishableKey)) {
      await _module.InvokeVoidAsync("initializeStripe", configResponse.PublishableKey, _dotNetRef);
    } else {
      throw new InvalidOperationException("Failed to retrieve Stripe publishable key from the server");
    }
  }

  public async Task<string> PreparePaymentIntent(decimal amount, string currency = "gbp", string description = "") {
    // Convert decimal to cents (long) for Stripe
    long pennies = (long)(amount * 100);

    PaymentIntentRequest request = new() {
      Amount = pennies,
      Currency = currency,
      Description = description
    };

    HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/payment/prepare-payment-intent", request);

    if (response.IsSuccessStatusCode) {
      PaymentIntentResponse? paymentIntentResponse = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
      if (paymentIntentResponse != null && !string.IsNullOrEmpty(paymentIntentResponse.PaymentIntentId)) {
        return paymentIntentResponse.PaymentIntentId;
      }
    }

    string errorContent = await response.Content.ReadAsStringAsync();
    throw new Exception($"Failed to prepare payment intent: {errorContent}");
  }

  public async Task<PaymentResult> SubmitPayment(string paymentMethodId, long amount) {
    ProcessPaymentRequest request = new() {
      PaymentMethodId = paymentMethodId,
      Amount = amount
    };

    HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/payment/process-payment", request);

    if (response.IsSuccessStatusCode) {
      PaymentResult result = await response.Content.ReadFromJsonAsync<PaymentResult>() ?? new();
      return result;
    }
    string errorContent = await response.Content.ReadAsStringAsync();
    return new PaymentResult {
      Success = false,
      Status = "failed",
      ErrorMessage = $"Payment processing failed: {errorContent}"
    };
  }

  [JSInvokable]
  public void PaymentMethodCreated(string paymentMethodJson) {
    // This method will be called from JavaScript when a payment method is created
    PaymentMethodResponse? paymentMethod = JsonSerializer.Deserialize<PaymentMethodResponse>(paymentMethodJson);

    // Store the payment method id or other relevant data
    // You might want to store this in a property that your Razor component can access
  }

  public async ValueTask DisposeAsync() {
    if (_module != null) {
      await _module.InvokeVoidAsync("cleanupStripe");
      await _module.DisposeAsync();
    }
    _dotNetRef.Dispose();
  }
}