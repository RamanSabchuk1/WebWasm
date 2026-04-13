using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class VehicleModal
{
	[Parameter]
	public required (Company, Driver)[] DriversWithCompany { get; set; }

	[Parameter]
	public bool IsEditMode { get; set; } = false;

	[Parameter]
	public Vehicle? InitialVehicle { get; set; }

	[Parameter]
	public required EventCallback<(Guid, CreateVehicle)> OnSubmit { get; set; }

	[Parameter]
	public required EventCallback OnCancel { get; set; }

	private string _model = string.Empty;
	private string _registrationNumber = string.Empty;
	private uint _vehicleWeight;
	private uint _loadCapacity;
	private string _photo = string.Empty;
	private Guid _selectedCompanyId;
	private Guid _selectedDriverId;

	protected override void OnParametersSet()
	{
		if (IsEditMode && InitialVehicle is not null)
		{
			_model = InitialVehicle.Model;
			_registrationNumber = InitialVehicle.RegistrationNumber;
			_vehicleWeight = InitialVehicle.VehicleWeight;
			_loadCapacity = InitialVehicle.LoadCapacity;
			_photo = InitialVehicle.Photo ?? string.Empty;
			_selectedDriverId = InitialVehicle.DriverId;
            _selectedCompanyId = GetCompanyId(InitialVehicle.DriverId);
		}
		else
		{
			Reset();
		}
	}

	private bool IsFormValid()
	{
		return !string.IsNullOrWhiteSpace(_model) &&
			   !string.IsNullOrWhiteSpace(_registrationNumber) &&
			   _vehicleWeight > 0 &&
			   _loadCapacity > 0 &&
			   _selectedDriverId != Guid.Empty;
	}

	private async Task SubmitForm()
	{
		if (!IsFormValid())
        {
            return;
        }

        var createVehicle = new CreateVehicle(_model, _registrationNumber, _vehicleWeight, _loadCapacity, _photo, _selectedDriverId);
		await OnSubmit.InvokeAsync((GetCompanyId(_selectedDriverId), createVehicle));
		Reset();
	}

	private void Reset()
	{
		_model = string.Empty;
		_registrationNumber = string.Empty;
		_vehicleWeight = 0;
		_loadCapacity = 0;
		_photo = string.Empty;
		_selectedCompanyId = Guid.Empty;
		_selectedDriverId = Guid.Empty;
	}

	private Guid GetCompanyId(Guid driverId)
	{
		foreach (var (company, driver) in DriversWithCompany)
		{
			if (driver.Id == driverId)
			{
				return company.Id;
			}
		}

		return Guid.Empty;
	}
}
