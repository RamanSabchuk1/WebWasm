using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebWasm;
using WebWasm.Helpers;
using WebWasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var settings = AppSettings.From(builder.Configuration);
builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient(ApiClient.HttpClientName, client => client.BaseAddress = settings.ApiBaseAddress);

var options = SerializationHelper.SerializerOptions();

builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<Preferences>();
// Interface texts of the chosen language for every component (LocalizedComponent.T).
builder.Services.AddCascadingValue(provider => provider.GetRequiredService<Preferences>().TextsSource);

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<LoadingService>();
builder.Services.AddSingleton<CashService>();
builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<LocalStorageAuthStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(provider => provider.GetRequiredService<LocalStorageAuthStateProvider>());

builder.Services.AddAuthorizationCore();

var host = builder.Build();
await host.Services.GetRequiredService<Preferences>().Initialize();
await host.RunAsync();
