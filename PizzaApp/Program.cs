using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PizzaApp.Components;
using PizzaApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<Routes>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The browser always calls the server that served the app. No per-device API URL.
var apiUrl = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddPizzaBrowserClient(apiUrl);

await builder.Build().RunAsync();
