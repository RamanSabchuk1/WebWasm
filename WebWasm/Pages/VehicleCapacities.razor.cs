using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

/// <summary>
/// SA-страница управления тоннажами машин (pricing overhaul, S5.0, паттерн D32).
/// Backend: GET/POST/PUT/DELETE /admin/vehicle-capacities (S4.5.3). Удаление — с выбором
/// «переназначить на другой вес» / «удалить машины (+ каскад слотов)» и предупреждением.
/// </summary>
public partial class VehicleCapacities : ComponentBase
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<VehicleCapacity> _capacities = [];

	private bool _showCreateDialog;
	private int _createWeightKg;
	private decimal _createMinPrice;
	private decimal _createMaxPrice;
	private string? _createError;

	private bool _showEditDialog;
	private int _editWeightKg;
	private decimal _editMinPrice;
	private decimal _editMaxPrice;
	private bool _editIsActive;
	private string? _editError;

	private bool _showDeleteDialog;
	private int _deleteWeightKg;
	private bool _deleteModeReassign = true;
	private int _reassignToWeightKg;
	private string? _deleteError;

	protected override async Task OnInitializedAsync()
	{
		await LoadCapacities();
	}

	private static string FormatWeight(int weightKg) =>
		weightKg % 1000 == 0 ? $"{weightKg / 1000} t" : $"{weightKg} kg";

	private async Task LoadCapacities()
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var capacities = await ApiClient.Get<VehicleCapacity[]>("admin/vehicle-capacities");
				_capacities = [.. capacities.OrderBy(x => x.WeightKg)];
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to load vehicle capacities: {ex.Message}");
			}
		});
	}

	private void OpenCreateDialog()
	{
		_createWeightKg = 0;
		_createMinPrice = 0;
		_createMaxPrice = 0;
		_createError = null;
		_showCreateDialog = true;
	}

	private void CloseCreateDialog() => _showCreateDialog = false;

	private async Task SubmitCreate()
	{
		if (_createWeightKg <= 0)
		{
			_createError = "Weight must be a positive number of kg.";
			return;
		}

		if (_createMaxPrice < _createMinPrice)
		{
			_createError = "Default Max Price must be greater than or equal to Min Price.";
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post("admin/vehicle-capacities", new CreateVehicleCapacityRequest(_createWeightKg, _createMinPrice, _createMaxPrice));
				ToastService.ShowSuccess($"Capacity {FormatWeight(_createWeightKg)} created.");
				_showCreateDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_createError = ex.Message;
			}
		});
	}

	private void OpenEditDialog(VehicleCapacity capacity)
	{
		_editWeightKg = capacity.WeightKg;
		_editMinPrice = capacity.DefaultMinPrice;
		_editMaxPrice = capacity.DefaultMaxPrice;
		_editIsActive = capacity.IsActive;
		_editError = null;
		_showEditDialog = true;
	}

	private void CloseEditDialog() => _showEditDialog = false;

	private async Task SubmitEdit()
	{
		if (_editMaxPrice < _editMinPrice)
		{
			_editError = "Default Max Price must be greater than or equal to Min Price.";
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Put($"admin/vehicle-capacities/{_editWeightKg}", new UpdateVehicleCapacityRequest(_editMinPrice, _editMaxPrice, _editIsActive));
				ToastService.ShowSuccess($"Capacity {FormatWeight(_editWeightKg)} updated.");
				_showEditDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_editError = ex.Message;
			}
		});
	}

	private void OpenDeleteDialog(VehicleCapacity capacity)
	{
		_deleteWeightKg = capacity.WeightKg;
		_deleteModeReassign = _capacities.Any(x => x.WeightKg != capacity.WeightKg);
		_reassignToWeightKg = _capacities.FirstOrDefault(x => x.WeightKg != capacity.WeightKg)?.WeightKg ?? 0;
		_deleteError = null;
		_showDeleteDialog = true;
	}

	private void CloseDeleteDialog() => _showDeleteDialog = false;

	private async Task SubmitDelete()
	{
		var endpoint = $"admin/vehicle-capacities/{_deleteWeightKg}";
		if (_deleteModeReassign)
		{
			if (_reassignToWeightKg <= 0 || _reassignToWeightKg == _deleteWeightKg)
			{
				_deleteError = "Choose a valid target weight for reassignment.";
				return;
			}

			endpoint += $"?reassignTo={_reassignToWeightKg}";
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete(endpoint);
				ToastService.ShowSuccess($"Capacity {FormatWeight(_deleteWeightKg)} deleted.");
				_showDeleteDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_deleteError = ex.Message;
			}
		});
	}
}
