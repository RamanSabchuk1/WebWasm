using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Vehicles
{
	private Vehicle[]? _vehicles;
	private (Company, Driver)[] _driverWithCompany = [];
	private readonly Guid _vehiclesKey = Guid.NewGuid();

	private bool _showCreateModal = false;
	private bool _showDeleteConfirm = false;
	private Vehicle? _vehicleToDelete;
	private string _deleteConfirmMessage = string.Empty;

	private async Task LoadData(bool useCash)
	{
		var allVehicles = await CashService.GetData<Vehicle>(useCash);
		var users = await CashService.GetData<User>(useCash);
		_driverWithCompany = await CashService.GetDriverWithCompany();

		// Map drivers to vehicles with user phone info
		_vehicles = allVehicles.MapDriversToVehicles(_driverWithCompany, users);
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
				if (companyId == Guid.Empty)
				{
					ToastService.ShowError("Cannot retreve a company ID");
					return;
				}

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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadData(true);
		}
	}
}
