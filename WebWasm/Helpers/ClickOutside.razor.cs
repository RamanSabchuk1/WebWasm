using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebWasm.Helpers;

public partial class ClickOutside : ComponentBase, IAsyncDisposable
{
	[Inject] private IJSRuntime JS { get; set; } = default!;

	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public EventCallback OnClickOutside { get; set; }

	private ElementReference _containerRef;
	private DotNetObjectReference<ClickOutside>? _dotNetHelper;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		_dotNetHelper = DotNetObjectReference.Create(this);
		try
		{
			await JS.InvokeVoidAsync("clickOutside.register", _containerRef, _dotNetHelper);
		}
		catch (JSException ex)
		{
			Console.WriteLine($"clickOutside.register failed: {ex.Message}");
		}
	}

	[JSInvokable]
	public Task InvokeClickOutside() => OnClickOutside.InvokeAsync();

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (_containerRef.Context != null)
		{
			try
			{
				await JS.InvokeVoidAsync("clickOutside.unregister", _containerRef);
			}
			catch (JSDisconnectedException)
			{
			}
		}

		_dotNetHelper?.Dispose();
	}
}
