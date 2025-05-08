using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using StripeInWasm2.Common.Models;

namespace StripeInWasm2.Client.Services;

public class StripeService : IAsyncDisposable {
  private readonly HttpClient _httpClient;
  private readonly IJSRuntime _jsRuntime;
  private IJSObjectReference? _module;
  private readonly DotNetObjectReference<StripeService> _dotNetRef;
  private readonly NavigationManager _navigationManager;

  public StripeService(HttpClient httpClient, IJSRuntime jsRuntime, NavigationManager navigationManager) {
    _httpClient = httpClient;
    _jsRuntime = jsRuntime;
    _dotNetRef = DotNetObjectReference.Create(this);
    _navigationManager = navigationManager;
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

  public async Task<PaymentResult> SubmitPayment(string paymentMethodId, long amount) {
    ProcessPaymentRequest request = new() {
      PaymentMethodId = paymentMethodId,
      Amount = amount,
      BaseUrl = _navigationManager.BaseUri
    };

    HttpResponseMessage response = await _httpClient.PostAsJsonAsync("/api/payment/process-payment", request);

    if (response.IsSuccessStatusCode) {
      PaymentResult result = await response.Content.ReadFromJsonAsync<PaymentResult>() ?? new();
      return result;
    }
    string errorContent = await response.Content.ReadAsStringAsync();
    return new PaymentResult {
      Status = PaymentResultStatuses.Error,
      Message = $"Payment processing failed: {errorContent}"
    };
  }

  public async ValueTask DisposeAsync() {
    if (_module != null) {
      await _module.InvokeVoidAsync("cleanupStripe");
      await _module.DisposeAsync();
    }
    _dotNetRef.Dispose();
  }
}