using StripeInWasm2.Server;
using StripeInWasm2.Server.Components;
using StripeInWasm2.Server.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped(sp => new HttpClient());
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<PaymentService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseWebAssemblyDebugging();
} else {
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.Use(async (context, next) => {
  if(!context.Response.Headers.ContainsKey("Permissions-Policy")) {
    context.Response.Headers.Add("Permissions-Policy", "payment=(self \"https://hooks.stripe.com\" \"https://js.stripe.com\" \"https://*.js.stripe.com\" \"https://api.stripe.com\" \"https://maps.googleapis.com\"), publickey-credentials-get=(self \"https://hooks.stripe.com\" \"https://js.stripe.com\" \"https://*.js.stripe.com\" \"https://api.stripe.com\" \"https://maps.googleapis.com\")");
  }
  await next();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveWebAssemblyRenderMode()
  .AddAdditionalAssemblies(typeof(StripeInWasm2.Client._Imports).Assembly);

app.MapGroup("/api/payment")
  .MapPaymentApi()
  .WithTags("Payment");

app.Run();