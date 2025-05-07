# Integrating Stripe with a Blazor WASM app

To run this sample, make sure you have a Stripe account and have set up your API keys. You can find the keys in your Stripe dashboard under Developers > API keys.

Then add the keys to `appSettings.json`...

```json
  "Stripe": {
    "SecretKey": "sk_test_whatever",
    "PublishableKey": "pk_test_whatever"
  }
```

Then you should be able to run.

## Caveat

As far as I can see, this works fine, but please do not use this code in production without testing it carefully. This is a sample I made while exploring the Stripe API, and I do not offer any guarantees that I got this right!

## Explanation

As far as I understand it, this is how it works...

The payment process is initiated from the `ProcessPayment` method in the [checkout component](https://github.com/MrYossu/IntegratingStripeWithBlazorWASM/blob/master/StripeInWasm2/StripeInWasm2.Client/Pages/Checkout.razor). This is a mock checkout page, where you can enter the transaction amount in the textbox.

We call Stripe's API to create a payment method, and return the status, Id, etc to the component. If that succeeded, we call our Stripe service to process the payment. We pass in the payment method Id and the amount.

If the card does not require 3D authentication, then this succeeds and we display the transaction Id on the page. In reality you would probably save this in your database and redirect to another page, but hey, this is a sample right? We get to do dumb things in a sample 😎.

If the card requires 3D authentication, then we show an `<iframe>` with the URL returned from Stripe's API as the source. In this sample, the `<iframe>` is plopped below the card controls. In a real app you would probably show this in a modal (which is why we refer to a modal in the code), see previous frivolous comment.

The user then has to complete the 3D authentication in the `<iframe>`.

Now, here is the bit that took me a lot longer than it really should have. Two things about the process bothered me and confused me...

1. It's all very nice using an `<iframe>` to display the 3D authentication part, but we need to get that information back to our component. How do we do that? Good question, and one that took me quite some time to figure.
2. Stripe require you to provide a redirect URL, which will be used after the 3D authentication process has completed. I don't want to be redirected, I want to stay right here.

The answer turned out to be quite simple, and actually quite neat.

We add a static HTML page to the app (see [post3d.htm](https://github.com/MrYossu/IntegratingStripeWithBlazorWASM/blob/master/StripeInWasm2/StripeInWasm2/wwwroot/post3d.htm)) and use that as the return URL.

>Why a static HTML page? Mainly because it only needs a very simple bit of JavaScript, and a Blazor component is overkill for that. As it happens, the JavaScript only seems to work when in static render mode, meaning that as soon as the WASM rendering kicks in, it fails to find the query string parameters, and so doesn't send the message. I never worked out why this happened, but as using a static HTML page seemed more sensible, I didn't bother pursuing the point.

The redirect URL is set in the [payment service](https://github.com/MrYossu/IntegratingStripeWithBlazorWASM/blob/master/StripeInWasm2/StripeInWasm2/Services/PaymentService.cs#L51) and uses the base URI passed from the client (as you can't get the base URI on the server as far as I know), so it will work without change on your local machine, on a test server, in production, in Hawaii[1], anywhere...

```c#
      PaymentIntentCreateOptions options = new() {
        Amount = request.Amount,
        Currency = "gbp",
        PaymentMethod = request.PaymentMethodId,
        Confirm = true,
        ConfirmationMethod = "automatic",
        ReturnUrl = $"{request.BaseUrl}post3d.html",
        PaymentMethodTypes = ["card"]
      };
```

The return URL needs to point at your HTML file, which in my case was called `post3d.html`. Feel free to change it to something more imaginative if you like.

>[1] - Why Hawaii? No real reason, just somewhere I've always fancied going!

The trick is that only the contents of the `<iframe>` are redirected to this static page. Mine just shows a message that says "Processing" but you'd probably spiff that up in production.

The page has some JavaScript that checks if there is a query string parameter named `payment_intent_client_secret`. This is one of the parameters that Stripe add when redirecting, and should always be there.

If we have this parameter, then this will contain the secret you need to confirm the payment.

> The name here is very confusing, as it made me think that the secret API key was being exposed, which would be quite a security risk. It turns out that it's nothing to do with that, and is perfectly safe to expose. Your secret API key will look something like `sk_test_somelongstringoflettersandnumbers`, whereas this secret key will look like `pi_someramdoncharcaters_secret_morerandomcharacters`

Anyway, once we have this value, we can pass it back to the component, which can confirm the payment.

So, how do we pass it back? Quite a clever trick here. I can say that as I didn't think of it! When the checkout component loads, it calls a bit of JavaScript that sets up a listener...

```c#
await _jsModule.InvokeVoidAsync("setupMessageListener", _objectReference);
```

The JavaScript listener receives messages, and acts accordingly. In our case, we look for messages that contain the secret key...

```js
export async function setupMessageListener(blazorObjectReference) {
  window.addEventListener('message', function (event) {
    if (typeof event.data === 'string' && event.data.startsWith('payment_intent_client_secret:')) {
      blazorObjectReference.invokeMethodAsync('HandlePostMessage', event.data);
    }
  });
};
```

Our HTMLpage does this by posting a message, which this listener picks up...

```js
document.addEventListener('DOMContentLoaded', function () {
  const urlParams = new URLSearchParams(queryString);
  if (urlParams.get('payment_intent_client_secret')) {
    window.parent.postMessage('payment_intent_client_secret:' + urlParams.get('payment_intent_client_secret'), '*');
  }
});
```

`HandlePost3DMessage`  checks if the incoming message is in the right format, and if so, calls the Stripe API to confirm the payment...

```c#
if (message.StartsWith("payment_intent_client_secret:")) {
  string secret = message.Substring(message.IndexOf(":") + 1);
  PaymentIntentDto paymentIntent = await _jsModule!.InvokeAsync<PaymentIntentDto>("retrievePaymentIntent", secret);
}
```

In this sample, you are left on the page, and should see a message telling you what happened. For some reason, the message doesn't show, but I didn't spend too much time on this, as I had got to where I needed to be, ie the component had all the information it needed to save the details to the database and redirect the user to a post-checkout page.