using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebWasm.Components;

public partial class UpdateNotification : ComponentBase, IAsyncDisposable
{
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

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
		_updateAvailable = true;
		StateHasChanged();
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
