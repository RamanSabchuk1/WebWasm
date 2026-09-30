using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebWasm.Components;

public partial class ActionsDropdown : ComponentBase
{
	[Inject] private IJSRuntime JS { get; set; } = default!;

	private ElementReference _triggerRef;
	private ElementReference _menuRef;
	private bool _open;
	private bool _measured;
	private bool _measureFailed;
	private bool _repositioned;
	private double _menuTop;
	private double _menuLeft;

	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public string Title { get; set; } = "Actions";
	[Parameter] public double MenuHeight { get; set; } = 310;

	// Hidden until measured to avoid a flash at a wrong position;
	// without coordinates (JS failure) falls back to CSS absolute positioning near the trigger.
	private string MenuStyle => (_measured, _measureFailed) switch
	{
		(true, _) => $"position: fixed; top: {_menuTop}px; left: {_menuLeft}px; display: flex;",
		(false, true) => "display: flex;",
		_ => "display: none;"
	};

	private async Task ToggleAsync()
	{
		if (_open)
		{
			Close();
			return;
		}

		_open = true;
		_measured = false;
		_measureFailed = false;
		_repositioned = false;

		var rect = await GetTriggerBox();
		if (rect is null)
		{
			_measureFailed = true;
			return;
		}

		// Position with estimated height first; refined to the real height in OnAfterRenderAsync.
		Place(rect, MenuHeight);
		_measured = true;
	}

	// Menu is rendered hidden until measured; first render with coordinates lets us refine
	// drop-up placement with the real menu height (item count varies per entity).
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_open || !_measured || _repositioned || _measureFailed)
		{
			return;
		}

		_repositioned = true;

		double? height = null;
		try
		{
			height = await JS.InvokeAsync<double?>("getElementHeight", _menuRef);
		}
		catch (JSException)
		{
		}

		if (height is null || await GetTriggerBox() is not { } rect)
		{
			return;
		}

		Place(rect, height.Value);
		StateHasChanged();
	}

	private async Task<BoundingBox?> GetTriggerBox()
	{
		try
		{
			return await JS.InvokeAsync<BoundingBox?>("getElementCoordinates", _triggerRef);
		}
		catch (JSException)
		{
			return null;
		}
	}

	// Below the trigger, or above it when there is no room below but there is above.
	private void Place(BoundingBox rect, double menuHeight)
	{
		var spaceBelow = rect.WindowHeight - rect.Bottom;
		_menuLeft = rect.Left - (210 - rect.Width);
		_menuTop = spaceBelow < menuHeight && rect.Top > menuHeight
			? rect.Top - menuHeight - 5
			: rect.Bottom + 5;
	}

	private void Close()
	{
		_open = false;
		_measured = false;
		_measureFailed = false;
	}

	private sealed class BoundingBox
	{
		public double Top { get; set; }
		public double Left { get; set; }
		public double Bottom { get; set; }
		public double Right { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double WindowHeight { get; set; }
	}
}
