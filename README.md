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

## Status
This is a work in progress, so no guarantees that it's right! At the time of first commit, it seems to work fine, except for cards that require 3D authentication. I'm still struggling with that bit 😎