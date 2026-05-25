using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace WebWasm.Components;

public partial class ModalShell : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public string ContainerClass { get; set; } = string.Empty;
	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public bool TopLayer { get; set; }

	private ElementReference _overlayRef;
	private bool _prevIsOpen;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (IsOpen && !_prevIsOpen)
		{
			_prevIsOpen = true;
			try { await _overlayRef.FocusAsync(); }
			catch { /* focus may fail in some scenarios */ }
		}
		else if (!IsOpen)
		{
			_prevIsOpen = false;
		}
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Escape" && OnClose.HasDelegate)
		{
			await OnClose.InvokeAsync();
		}
	}
}
