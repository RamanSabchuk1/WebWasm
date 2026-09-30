using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class LocationMapPicker : LeafletMapBase
{
	[Parameter] public Location? InitialLocation { get; set; }
	[Parameter] public EventCallback<Location> OnLocationChanged { get; set; }
	[Parameter] public string? MapHeight { get; set; }
	[Parameter] public string? Title { get; set; }
	[Parameter] public bool ShowInputs { get; set; } = true;

	private DotNetObjectReference<LocationMapPicker>? _dotNetRef;
	private double _latitude;
	private double _longitude;

	protected override string ModulePath => "./js/location-picker.js";

	// Before the first render, so the coordinate inputs show the start point right away.
	protected override void OnInitialized()
	{
		// Default to Minsk, Belarus
		(_latitude, _longitude) = InitialLocation is { } location ? (location.Latitude, location.Longitude) : (53.9045, 27.5615);
	}

	protected override (string Function, object?[] Args) MapInit() =>
		("initLocationPicker", [_latitude, _longitude, _dotNetRef = DotNetObjectReference.Create(this)]);

	[JSInvokable]
	public async Task OnMapClick(double lat, double lng)
	{
		_latitude = lat;
		_longitude = lng;
		await NotifyLocationChanged();
		StateHasChanged();
	}

	private async Task UpdateMarkerFromInput()
	{
		if (MapInstance is null)
		{
			return;
		}

		try
		{
			await MapInstance.InvokeVoidAsync("updateMarker", _latitude, _longitude);
			await NotifyLocationChanged();
		}
		catch (JSException ex)
		{
			OnMapError($"Error updating marker: {ex.Message}");
		}
	}

	private Task NotifyLocationChanged() => OnLocationChanged.InvokeAsync(new Location(_longitude, _latitude));

	public override async ValueTask DisposeAsync()
	{
		await base.DisposeAsync();
		_dotNetRef?.Dispose();
	}
}
