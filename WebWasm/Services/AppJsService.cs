using Microsoft.JSInterop;

namespace WebWasm.Services;

public class AppJsService(IJSRuntime jSRuntime)
{
    private bool isInitialized = false;
    private IJSObjectReference? appJsModule;
    
    public async ValueTask<IJSObjectReference> GetAppJsModule()
    {
        if (isInitialized)
        {
            return appJsModule ?? throw new InvalidOperationException("JS module is not loaded.");
        }

        appJsModule = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./app.js");
        isInitialized = true;
        return appJsModule!;
    }
}
