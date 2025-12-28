using Microsoft.JSInterop;

namespace WebWasm.Services;

public class AppJsSerrivce(IJSRuntime jSRuntime)
{
    private bool isInizialized = false;
    private IJSObjectReference? appJsModule;
    
    public async ValueTask<IJSObjectReference> GetAppJsModule()
    {
        if (isInizialized)
        {
            return appJsModule ?? throw new InvalidOperationException("JS module is not loaded.");
        }

        appJsModule = await jSRuntime.InvokeAsync<IJSObjectReference>("import", "./app.js");
        isInizialized = true;
        return appJsModule!;
    }
}
