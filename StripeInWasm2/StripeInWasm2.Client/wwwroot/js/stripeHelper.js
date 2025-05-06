// stripeHelper.js
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

    // Notify .NET about the payment method
    if (dotNetHelper) {
      dotNetHelper.invokeMethodAsync('PaymentMethodCreated', JSON.stringify(paymentMethod));
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

// This function is not needed anymore as we're using the card element directly
export function updatePaymentAmount(amount) {
  // No need to update amount with card element
  console.log(`Payment amount: ${amount} (stored for server-side use)`);
}

export function cleanupStripe() {
  // Cleanup logic if needed
  stripe = null;
  elements = null;
  cardElement = null;
  dotNetHelper = null;
}