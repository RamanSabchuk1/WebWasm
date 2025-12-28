using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebWasm;
using WebWasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<AppJsSerrivce>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<LocalStorageAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<LocalStorageAuthStateProvider>());

builder.Services.AddAuthorizationCore();


await builder.Build().RunAsync();
