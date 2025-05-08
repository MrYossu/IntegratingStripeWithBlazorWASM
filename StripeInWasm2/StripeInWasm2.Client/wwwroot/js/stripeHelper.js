let stripe;
let elements;
let cardElement;
let dotNetHelper;

export function initializeStripe(publishableKey, dotNetRef) {
  // Store the .NET reference for callbacks
  dotNetHelper = dotNetRef;

  // Load Stripe.js
  if (!window.Stripe) {
    const script = document.createElement('script');
    script.src = 'https://js.stripe.com/v3/';
    script.async = true;
    document.head.appendChild(script);

    return new Promise((resolve) => {
      script.onload = () => {
        stripe = window.Stripe(publishableKey);
        setupElements();
        resolve();
      };
    });
  } else {
    stripe = window.Stripe(publishableKey);
    setupElements();
    return Promise.resolve();
  }
}

function setupElements() {
  // Create elements instance
  elements = stripe.elements();

  // Create the Card Element
  cardElement = elements.create('card', {
    style: {
      base: {
        fontSize: '16px',
        color: '#32325d',
        fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
        fontSmoothing: 'antialiased',
        '::placeholder': {
          color: '#aab7c4'
        }
      },
      invalid: {
        color: '#fa755a',
        iconColor: '#fa755a'
      }
    }
  });

  // Wait for the DOM to be ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
      mountCardElement();
    });
  } else {
    mountCardElement();
  }
}

function mountCardElement() {
  const cardElementContainer = document.getElementById('card-element');
  if (cardElementContainer) {
    cardElement.mount('#card-element');

    // Add event listener for validation errors
    cardElement.on('change', function (event) {
      const displayError = document.getElementById('card-errors');
      if (displayError) {
        if (event.error) {
          displayError.textContent = event.error.message;
        } else {
          displayError.textContent = '';
        }
      }
    });
  } else {
    // Element not found yet, pause and try again
    setTimeout(mountCardElement, 100);
  }
}

export async function createPaymentMethod() {
  if (!stripe || !cardElement) {
    return {
      success: false,
      error: 'Stripe not initialized properly'
    };
  }

  try {
    // Create payment method
    const { error, paymentMethod } = await stripe.createPaymentMethod({
      type: 'card',
      card: cardElement,
      billing_details: {} // Can be populated with customer info if needed
    });

    if (error) {
      return {
        success: false,
        error: error.message
      };
    }

    return {
      success: true,
      paymentMethodId: paymentMethod.id
    };
  } catch (e) {
    return {
      success: false,
      error: e.message
    };
  }
}

let messageListener = null;
export async function setupMessageListener(blazorObjectReference) {
  messageListener = function (event) {
    if (typeof event.data === 'string' && event.data.startsWith('payment_intent_client_secret:')) {
      blazorObjectReference.invokeMethodAsync('HandlePost3DMessage', event.data);
    }
  };
  window.addEventListener('message', messageListener);
};

export async function retrievePaymentIntent(secret) {
  const result = await stripe.retrievePaymentIntent(secret);
  if (result.error) {
    return new { Status: "failed" };
  } else {
    return result.paymentIntent;
  }
}

export function cleanup() {
  stripe = null;
  elements = null;
  cardElement = null;
  dotNetHelper = null;
  if (messageListener) {
    window.removeEventListener('message', messageListener);
    messageListener = null;
  }
}