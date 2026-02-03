using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class LoadingPlaceModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public LoadingPlace? EditingLoadingPlace { get; set; }
	[Parameter] public List<MaterialType> MaterialTypes { get; set; } = [];
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<MutateLoadingPlace> OnSubmit { get; set; }

	private string _name = string.Empty;
	private Location? _location;
	private Guid _materialTypeId = Guid.Empty;
	private decimal _cost = 0;
	private int _volume = 0;
	private string _errorMessage = string.Empty;
	private LocationMapPicker? _locationPicker;

	protected override void OnParametersSet()
	{
		if (IsOpen)
		{
			if (EditingLoadingPlace is not null)
			{
				_name = EditingLoadingPlace.Name;
				_location = EditingLoadingPlace.Location;
				_materialTypeId = EditingLoadingPlace.MaterialType?.Id ?? Guid.Empty;
				_cost = EditingLoadingPlace.Cost;
				_volume = EditingLoadingPlace.Volume;
			}
			else
			{
				ResetForm();
			}
		}
	}

	private void ResetForm()
	{
		_name = string.Empty;
		_location = null;
		_materialTypeId = Guid.Empty;
		_cost = 0;
		_volume = 0;
		_errorMessage = string.Empty;
	}

	private void HandleLocationChanged(Location location)
	{
		_location = location;
	}

	private bool IsValid =>
		!string.IsNullOrWhiteSpace(_name) &&
		_location is not null &&
		_materialTypeId != Guid.Empty &&
		_cost > 0 &&
		_volume > 0;

	private async Task HandleSubmit()
	{
		if (!IsValid)
		{
			_errorMessage = "Please fill in all required fields";
			return;
		}

		if (_location is null)
		{
			_errorMessage = "Please select a location on the map";
			return;
		}

		var mutateLoadingPlace = new MutateLoadingPlace(
			_name,
			_location,
			_materialTypeId,
			_cost,
			_volume
		);

		await OnSubmit.InvokeAsync(mutateLoadingPlace);
		ResetForm();
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}

	private bool IsEditMode => EditingLoadingPlace is not null;
}
