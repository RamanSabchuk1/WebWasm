using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class UpdateNotification : ComponentBase, IAsyncDisposable
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;

	private bool _updateAvailable = false;
	private DotNetObjectReference<UpdateNotification>? _objRef;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_objRef = DotNetObjectReference.Create(this);
			await JSRuntime.InvokeVoidAsync("serviceWorkerInterop.initialize", _objRef);
		}
	}

	[JSInvokable]
	public void OnUpdateAvailable(string version)
	{
        Console.WriteLine($"current version {version}");
		_updateAvailable = true;
		StateHasChanged();
	}

	[JSInvokable]
	public void OnForegroundMessage(System.Text.Json.JsonElement payload)
	{
		var title = "New Message";
		var body = "";

		// Try to get title/body from 'notification' property
		if (payload.TryGetProperty("notification", out var notification))
		{
			if (notification.TryGetProperty("title", out var t))
            {
                title = t.GetString() ?? title;
            }

            if (notification.TryGetProperty("body", out var b))
            {
                body = b.GetString() ?? body;
            }
        }
		// Fallback to 'data' property
		else if (payload.TryGetProperty("data", out var data))
		{
			if (data.TryGetProperty("title", out var t))
            {
                title = t.GetString() ?? title;
            }

            if (data.TryGetProperty("body", out var b))
            {
                body = b.GetString() ?? body;
            }
        }

		ToastService.ShowInfo($"{title}: {body}");
	}

	private async Task ReloadApp()
	{
		await JSRuntime.InvokeVoidAsync("location.reload");
	}

	private void DismissUpdate()
	{
		_updateAvailable = false;
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (_objRef != null)
		{
			await JSRuntime.InvokeVoidAsync("serviceWorkerInterop.dispose");
			_objRef.Dispose();
		}
	}
}
