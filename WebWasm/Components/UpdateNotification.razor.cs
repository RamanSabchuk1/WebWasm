using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class UpdateNotification : IAsyncDisposable
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
			try
			{
				await JSRuntime.InvokeVoidAsync("serviceWorkerInterop.initialize", _objRef);
			}
			catch (JSException ex)
			{
				Console.WriteLine($"serviceWorkerInterop.initialize failed: {ex.Message}");
			}
		}
	}

	[JSInvokable]
	public void OnUpdateAvailable(string version)
	{
		_updateAvailable = true;
		StateHasChanged();
	}

	private async Task ReloadApp()
	{
		try
		{
			await JSRuntime.InvokeVoidAsync("serviceWorkerInterop.applyUpdate");
		}
		catch (JSException ex)
		{
			ToastService.ShowError(T.Shell.UpdateFailed(ex.Message));
		}
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
			try
			{
				await JSRuntime.InvokeVoidAsync("serviceWorkerInterop.dispose");
			}
			catch (JSDisconnectedException)
			{
			}
			_objRef.Dispose();
		}
	}
}
