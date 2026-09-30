using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class PolygonDrawer : LeafletMapBase
{
	[Parameter] public ICollection<Location>? InitialPoints { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }
	[Parameter] public EventCallback<ICollection<Location>> OnConfirm { get; set; }

	private List<Location> _points = [];
	private string _errorMessage = string.Empty;
	private DotNetObjectReference<PolygonDrawer>? _dotNetRef;

	protected override string ModulePath => "./js/polygon-drawer.js";

	// The drawer shows map errors to the user: without the map nothing can be drawn.
	protected override void OnMapError(string message)
	{
		_errorMessage = message;
		StateHasChanged();
	}

	protected override (string Function, object?[] Args) MapInit()
	{
		if (InitialPoints?.Count > 0)
		{
			_points = [.. InitialPoints];
		}

		return ("initDrawingMap", [PointsData(), _dotNetRef = DotNetObjectReference.Create(this)]);
	}

	private List<object> PointsData() => [.. _points.Select(p => new { lat = p.Latitude, lng = p.Longitude })];

	[JSInvokable]
	public async Task OnMapClick(double lat, double lng)
	{
		_points.Add(new Location(lng, lat));
		await RedrawPolygon();
		StateHasChanged();
	}

	[JSInvokable]
	public async Task OnPointDrag(int index, double lat, double lng)
	{
		if (index >= 0 && index < _points.Count)
		{
			_points[index] = new Location(lng, lat);
			await RedrawPolygon();
			StateHasChanged();
		}
	}

	private async Task RedrawPolygon()
	{
		if (MapInstance is null)
		{
			return;
		}

		try
		{
			await MapInstance.InvokeVoidAsync("redrawPolygon", PointsData());
		}
		catch (JSException ex)
		{
			Console.WriteLine($"Error redrawing polygon: {ex.Message}");
		}
	}

	private void DeletePoint(int index)
	{
		if (index >= 0 && index < _points.Count)
		{
			_points.RemoveAt(index);
			_ = RedrawPolygon();
		}
	}

	private void UndoLastPoint()
	{
		if (_points.Count > 0)
		{
			_points.RemoveAt(_points.Count - 1);
			_ = RedrawPolygon();
		}
	}

	private void ClearAll()
	{
		_points.Clear();
		_ = RedrawPolygon();
	}

	private async Task HandleConfirm()
	{
		if (_points.Count < 3)
		{
			_errorMessage = "A polygon requires at least 3 points.";
			return;
		}

		await OnConfirm.InvokeAsync(_points);
	}

	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();
		_dotNetRef?.Dispose();
	}
}
