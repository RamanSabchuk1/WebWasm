using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

/// <summary>
/// SA-страница управления тоннажами машин (pricing overhaul, S5.0, паттерн D32).
/// Backend: GET/POST/PUT/DELETE /admin/vehicle-capacities (S4.5.3). Удаление — с выбором
/// «переназначить на другой вес» / «удалить машины (+ каскад слотов)» и предупреждением.
/// </summary>
public partial class VehicleCapacities
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

	private bool HasReassignTarget => _capacities.Any(x => x.WeightKg != _deleteWeightKg);

	private string FormatWeight(int weightKg) => T.Pricing.Weight(weightKg);

	private async Task LoadCapacities()
	{
		await LoadingService.Run(ToastService, async () =>
		{
			var capacities = await ApiClient.Get<VehicleCapacity[]>("admin/vehicle-capacities");
			_capacities = [.. capacities.OrderBy(x => x.WeightKg)];
		}, T.Capacities.LoadFailed);
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
			_createError = T.Capacities.WeightPositive;
			return;
		}

		if (_createMaxPrice < _createMinPrice)
		{
			_createError = T.Capacities.MaxBelowMin;
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post("admin/vehicle-capacities", new CreateVehicleCapacityRequest(_createWeightKg, _createMinPrice, _createMaxPrice));
				ToastService.ShowSuccess(T.Capacities.Created(FormatWeight(_createWeightKg)));
				_showCreateDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_createError = T.Capacities.CreateFailed(ex.Message);
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
			_editError = T.Capacities.MaxBelowMin;
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Put($"admin/vehicle-capacities/{_editWeightKg}", new UpdateVehicleCapacityRequest(_editMinPrice, _editMaxPrice, _editIsActive));
				ToastService.ShowSuccess(T.Capacities.Updated(FormatWeight(_editWeightKg)));
				_showEditDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_editError = T.Capacities.SaveFailed(ex.Message);
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
				_deleteError = T.Capacities.ChooseTarget;
				return;
			}

			endpoint += $"?reassignTo={_reassignToWeightKg}";
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete(endpoint);
				ToastService.ShowSuccess(T.Capacities.Deleted(FormatWeight(_deleteWeightKg)));
				_showDeleteDialog = false;
				await LoadCapacities();
			}
			catch (Exception ex)
			{
				_deleteError = T.Capacities.DeleteFailed(ex.Message);
			}
		});
	}
}
