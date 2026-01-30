using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class PolygonDrawer : ComponentBase, IAsyncDisposable
{
	[Inject] protected IJSRuntime JS { get; set; } = default!;

	[Parameter] public ICollection<Location>? InitialPoints { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }
	[Parameter] public EventCallback<ICollection<Location>> OnConfirm { get; set; }

	private ElementReference _mapElement;
	private IJSObjectReference? _jsModule;
	private IJSObjectReference? _mapInstance;
	private List<Location> _points = [];
	private string _errorMessage = string.Empty;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadMapScript();
			await InitializeMap();
		}
	}

	private async Task LoadMapScript()
	{
		try
		{
			_jsModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/polygon-drawer.js");
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to load map: {ex.Message}";
		}
	}

	private async Task InitializeMap()
	{
		if (_jsModule is null)
		{
			return;
		}

		try
		{
			// Load initial points if provided
			if (InitialPoints?.Count > 0)
			{
				_points = InitialPoints.ToList();
			}

			var pointsData = _points.Select(p => new { lat = p.Latitude, lng = p.Longitude }).ToList();

			_mapInstance = await _jsModule.InvokeAsync<IJSObjectReference>(
				"initDrawingMap",
				_mapElement,
				pointsData,
				DotNetObjectReference.Create(this)
			);
		}
		catch (Exception ex)
		{
			_errorMessage = $"Failed to initialize map: {ex.Message}";
		}
	}

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
		if (_jsModule is null || _mapInstance is null)
		{
			return;
		}

		try
		{
			var pointsData = _points.Select(p => new { lat = p.Latitude, lng = p.Longitude }).ToList();
			await _mapInstance.InvokeVoidAsync("redrawPolygon", pointsData);
		}
		catch (Exception ex)
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

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		if (_mapInstance is not null)
		{
			try
			{
				await _mapInstance.InvokeVoidAsync("dispose");
				await _mapInstance.DisposeAsync();
			}
			catch { }
		}

		if (_jsModule is not null)
		{
			await _jsModule.DisposeAsync();
		}
	}
}
