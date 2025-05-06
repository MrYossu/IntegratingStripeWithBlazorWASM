using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StripeInWasm2.Client.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<StripeService>();

await builder.Build().RunAsync();
