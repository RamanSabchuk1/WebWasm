using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebWasm;
using WebWasm.Helpers;
using WebWasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient();

var options = SerializationHelper.SerializerOptions();

builder.Services.AddBlazoredLocalStorageAsSingleton(o => o.JsonSerializerOptions = options);

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<AppJsService>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<LoadingService>();
builder.Services.AddSingleton<CashService>();
builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<LocalStorageAuthStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(provider => provider.GetRequiredService<LocalStorageAuthStateProvider>());

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();