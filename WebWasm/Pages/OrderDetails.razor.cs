using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class OrderDetails(ApiClient apiClient, CashService cashService, LoadingService loadingService, ToastService toastService)
{
	[Parameter]
	public Guid Id { get; set; }

	private Order? _order;
	private CalculationInfo? _calculationInfo;
	private CalculationInfo[] _allCalculationInfo = [];
	private Company[] _companies = [];
	private Driver[] _drivers = [];
	private Level[] _levels = [];
	
	private Dictionary<Guid, Guid> _selectedCompanies = [];
	private Dictionary<Guid, Guid> _selectedDrivers = [];

	// For Status Change
	private OrderStatus _newStatus;
	private string _newState = string.Empty;

	// Confirmation Dialog
	private bool _isConfirmOpen;
	private string _confirmTitle = "Confirm Action";
	private string _confirmMessage = "Are you sure you want to proceed?";
	private Func<Task>? _pendingAction;

    private LocationMapPicker? _locationPicker;

    protected override async Task OnInitializedAsync()
	{
		await FetchData();
		await LoadOrder();
	}

	private async Task LoadOrder()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				_order = await apiClient.Get<Order>($"Orders/{Id}");
				if (_order is null)
				{
					toastService.ShowError("Order NotFound");
					return;
				}
				
				if (_order.Deliveries != null)
				{
					foreach (var d in _order.Deliveries)
					{
						if (!_selectedDrivers.ContainsKey(d.Id))
						{
							_selectedDrivers[d.Id] = d.Driver?.Id ?? Guid.Empty;
						}

						if (!_selectedCompanies.ContainsKey(d.Id))
						{
							_selectedCompanies[d.Id] = Guid.Empty;
						}
					}
				}

				_calculationInfo = _allCalculationInfo.FirstOrDefault(c => c.Id == Id);
				_newStatus = _order.Status;
				_newState = _order.State ?? string.Empty;
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Error loading order: {ex.Message}");
			}
		});
	}

	private async ValueTask FetchData()
	{
		_companies = await cashService.GetData<Company>();
		_drivers = await cashService.GetData<Driver>();
		_allCalculationInfo = await cashService.GetData<CalculationInfo>();
		_levels = [.. (await cashService.GetData<Region>()).SelectMany(r => r.Levels)];
    }

	private void RequestResetPayments()
	{
		_confirmTitle = "Reset Payments";
		_confirmMessage = "Are you sure you want to reset all payments for this order? This action cannot be undone.";
		_pendingAction = ResetPayments;
		_isConfirmOpen = true;
	}

	private async Task ResetPayments()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Orders/{Id}/payments/reset");
				toastService.ShowSuccess("Payments reset successfully");
				await LoadOrder(); // Refresh
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Error resetting payments: {ex.Message}");
			}
		});
	}

	private void RequestAcceptDelivery(Guid deliveryId, Guid companyId, Guid driverId)
	{
		if (companyId == Guid.Empty || driverId == Guid.Empty)
		{
			toastService.ShowError("Please select a company and a driver.");
			return;
		}

		_confirmTitle = "Accept Delivery";
		_confirmMessage = "Are you sure you want to assign this delivery to the selected driver?";
		_pendingAction = async () => await AcceptDelivery(deliveryId, companyId, driverId);
		_isConfirmOpen = true;
	}

	private async Task AcceptDelivery(Guid deliveryId, Guid companyId, Guid driverId)
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Orders/{Id}/accept?companyId={companyId}&deliveryId={deliveryId}&driverId={driverId}");
				toastService.ShowSuccess("Delivery accepted successfully");
				await LoadOrder();
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Error accepting delivery: {ex.Message}");
			}
		});
	}

	private void RequestUpdateStatus()
	{
		_confirmTitle = "Update Status";
		_confirmMessage = $"Are you sure you want to update the status to {_newStatus}?";
		_pendingAction = UpdateStatus;
		_isConfirmOpen = true;
	}

	private async Task UpdateStatus()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Orders/{Id}/status?status={_newStatus}&state={Uri.EscapeDataString(_newState)}");
				toastService.ShowSuccess("Status updated successfully");
				await LoadOrder();
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Error updating status: {ex.Message}");
			}
		});
	}

	private async Task OnConfirmDialog()
	{
		_isConfirmOpen = false;
		if (_pendingAction != null)
		{
			await _pendingAction.Invoke();
			_pendingAction = null;
		}
	}

	private void OnCancelDialog()
	{
		_isConfirmOpen = false;
		_pendingAction = null;
	}

	private static string GetDriverName(Driver driver)
	{
		var name = $"{driver.UserInfo?.FirstName} {driver.UserInfo?.LastName}".Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = string.IsNullOrEmpty(driver.UserInfo?.MobilePhone) ? "Unknown Driver" : driver.UserInfo.MobilePhone;
		}
	
		return name;
	}
}
