using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Vehicles
{
	private Vehicle[]? _vehicles;
	private Company[] _companies = [];
	private Driver[] _drivers = [];
	private readonly Guid _vehiclesKey = Guid.NewGuid();

	private bool _showCreateModal = false;
	private bool _showDeleteConfirm = false;
	private Vehicle? _vehicleToDelete;
	private string _deleteConfirmMessage = string.Empty;

	private async Task LoadData(bool useCash)
	{
		var allVehicles = await CashService.GetData<Vehicle>(useCash);
		_companies = await CashService.GetData<Company>(useCash);
		_drivers = await CashService.GetData<Driver>(useCash);
		var users = await CashService.GetData<User>(useCash);

		// Map drivers to vehicles with user phone info
		_vehicles = MapDriversToVehicles(allVehicles, _drivers, users);
		StateHasChanged();
	}

	private void OpenCreateModal()
	{
		_showCreateModal = true;
	}

	private void CloseCreateModal()
	{
		_showCreateModal = false;
	}

	private async Task CreateVehicle((Guid, CreateVehicle) data)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
            try
			{
				var (companyId, createVehicle) = data;
				await ApiClient.Post<CreateVehicle, Vehicle>($"Companies/vehicle?companyId={companyId}", createVehicle);
				ToastService.ShowSuccess("Vehicle created successfully");
				CloseCreateModal();
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create vehicle: {ex.Message}");
			}
		});
	}

	private void ShowDeleteConfirmation(Vehicle vehicle)
	{
		_vehicleToDelete = vehicle;
		_deleteConfirmMessage = $"Are you sure you want to delete the vehicle '{vehicle.Model}' ({vehicle.RegistrationNumber})?";
		_showDeleteConfirm = true;
	}

	private async Task ConfirmDelete()
	{
		if (_vehicleToDelete == null)
        {
            return;
        }

        await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete($"Companies/vehicle?vehicleId={_vehicleToDelete.Id}");
				ToastService.ShowSuccess("Vehicle deleted successfully");
				CancelDelete();
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete vehicle: {ex.Message}");
			}
		});
	}

	private void CancelDelete()
	{
		_showDeleteConfirm = false;
		_vehicleToDelete = null;
		_deleteConfirmMessage = string.Empty;
	}

	private static Vehicle[] MapDriversToVehicles(Vehicle[] vehicles, Driver[] drivers, User[] users)
	{
		var driverDict = drivers.ToDictionary(d => d.Id);
		var userDict = users.ToDictionary(u => u.UserInfo.Id);

		return [.. vehicles.Select(vehicle =>
		{
			if (vehicle.DriverId != Guid.Empty && driverDict.TryGetValue(vehicle.DriverId, out var driver))
			{
				// Enrich driver with phone from User/UserInfo
				var enrichedDriver = driver;
				if (driver.UserInfo?.Id != Guid.Empty && 
					driver.UserInfo != null &&
					userDict.TryGetValue(driver.UserInfo.Id, out var user))
				{
					enrichedDriver = new Driver(
						driver.Id,
						driver.Photo,
						vehicle,
						new UserInfo(
							driver.UserInfo.Id,
							user.UserInfo.FirstName,
							user.UserInfo.MiddleName,
							user.UserInfo.LastName,
							user.UserInfo.MobilePhone,
							driver.UserInfo.Company
						)
					);
				}

				// Create new vehicle instance with mapped driver
				return new Vehicle(
					vehicle.Id,
					vehicle.DriverId,
					vehicle.CompanyId,
					vehicle.Model,
					vehicle.RegistrationNumber,
					vehicle.VehicleWeight,
					vehicle.LoadCapacity,
					vehicle.Photo,
					enrichedDriver
				);
			}
			return vehicle;
		})];
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
            await LoadData(true);
        }
    }
}
