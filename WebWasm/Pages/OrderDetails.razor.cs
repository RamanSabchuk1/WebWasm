using Microsoft.AspNetCore.Components;
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
	private Level[] _levels = [];
	private (Company, Driver)[] _driversWithCompany = [];
	private readonly Dictionary<double, Guid> _selectedDriverIds = [];

	// For Status Change
	private OrderStatus _newStatus;
	private string _newState = string.Empty;

	// Confirmation Dialog
	private bool _isConfirmOpen;
	private string _confirmTitle = "Confirm Action";
	private string _confirmMessage = "Are you sure you want to proceed?";
	private Func<Task>? _pendingAction;

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
		_allCalculationInfo = await cashService.GetData<CalculationInfo>();
		_levels = [.. (await cashService.GetData<Region>()).SelectMany(r => r.Levels)];
        _driversWithCompany = await cashService.GetDriverWithCompany();
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

	private void RequestAcceptDelivery(double weight)
	{
		_selectedDriverIds.TryGetValue(weight, out var selectedDriverId);

		var companyId = GetCompanyId(selectedDriverId);
		if (selectedDriverId == Guid.Empty)
		{
			toastService.ShowError("Please select a driver.");
			return;
		}

		if (companyId == Guid.Empty)
		{
			toastService.ShowError("Please select a company.");
			return;
		}

		_confirmTitle = "Accept Delivery";
		_confirmMessage = $"Are you sure you want to assign this delivery with weight {weight} to the selected driver?";
		_pendingAction = async () => await AcceptDelivery(weight, companyId, selectedDriverId);
		_isConfirmOpen = true;
	}

	private async Task AcceptDelivery(double weight, Guid companyId, Guid driverId)
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Post($"Orders/{Id}/accept?companyId={companyId}&weight={weight}&driverId={driverId}");
				toastService.ShowSuccess("Delivery accepted successfully");
				_selectedDriverIds.Remove(weight);
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

    private Guid GetCompanyId(Guid driverId)
    {
        foreach (var (company, driver) in _driversWithCompany)
        {
            if (driver.Id == driverId)
            {
                return company.Id;
            }
        }

        return Guid.Empty;
    }

    private string GetAdminState()
	{
		if (_order is null)
		{
			return string.Empty;
		}

		return _order.Status switch
        {
            OrderStatus.Draft => "Заказ почемуто в мусорном статусе 🗑️",
            OrderStatus.WaitingApprove => _order.PreferredDeliveryTime.Date == DateTime.Now.Date ? "⚠️ Заказ нужно выполнить уже сегодня а он еще не ПРИНЯТ похоже это мусор" : _order.Created < DateTime.Now.AddHours(2) ? "Заказ всё еще не принимают 🧲" : "Новый заказ создан ✒️",
            OrderStatus.PaymentPending => _order.PreferredDeliveryTime.Date == DateTime.Now.Date ? "⚠️ Заказ нужно выполнить уже сегодня а он еще не ОПЛАЧЕН похоже это мусор" : "Заказ всё еще не оплачен 💸",
            OrderStatus.Active => _order.PreferredDeliveryTime.Date == DateTime.Now.Date ? " Сегодня выполнение заказа 📦" : " Active order 📝",
            OrderStatus.Completed => "Считаем наши денюзки 🤑, не забываем оплатить Перевозчикам/Поставщикам",
            OrderStatus.CorruptedPayment => "💀 надо чтото сделать с этим ⏰",
            OrderStatus.Cancelled => "Увы но отмена 😢",
            OrderStatus.Archived => "Уже и не вспомнить что с ним было 😅",
            OrderStatus.Deleted => "👷 уже нет",
            OrderStatus.PaymentInProgress => "📣 обрабатываем платёж надеюсь всё ок 🤞 статус поменяется быстро",
			_ => "There is no state 🗽"
        };
	}
}
