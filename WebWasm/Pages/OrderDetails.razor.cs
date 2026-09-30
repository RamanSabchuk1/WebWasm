using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Helpers;
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
	private DriverSlot[] _driverSlots = [];
	private Vehicle[] _vehicles = [];
	private Level[] _levels = [];
	private static readonly OrderStatus[] _allStatuses = Enum.GetValues<OrderStatus>();
	private (Company, Driver)[] _driversWithCompany = [];
	private readonly Dictionary<double, Guid> _selectedDriverIds = [];
	private bool _userDetailsOpen;
	private string _userDetailsButtonName = "Show Details";

	private Driver? _driverInfo;

	// For Status Change
	private OrderStatus _newStatus;
	private string _newState = string.Empty;

	// Confirmation Dialog
	private readonly ConfirmState _confirm = new();

	// S5.2: SA-правка цены (S4.2) и PreferredDeliveryTime (S4.3)
	private bool _showPriceEdit;
	private decimal? _editMaterialCost;
	private decimal? _editCommission;
	// S5: суммарная стоимость доставок с НДС (Σ DeliveryInfo.TotalPrice).
	private decimal? _editDeliveryCost;
	private decimal? _originalDeliveryCost;
	private int _deliveryCount;
	private bool _hasAcceptedDelivery;
	private string? _priceEditError;

	private bool _showTimeEdit;
	private DateTime _editPreferredDeliveryTime;
	private string? _timeEditError;

	private void OpenPriceEdit()
	{
		if (_calculationInfo is null)
		{
			return;
		}

		_editMaterialCost = _calculationInfo.MaterialCost;
		_editCommission = _calculationInfo.Commission;
		_deliveryCount = _calculationInfo.DeliveryInfo.Length;
		_originalDeliveryCost = _calculationInfo.DeliveryInfo.Sum(x => x.TotalPrice);
		_editDeliveryCost = _originalDeliveryCost;
		// Предупреждаем, если доставка уже у водителя: правка меняет его вознаграждение.
		_hasAcceptedDelivery = _order?.Deliveries?.Any(d => d.Driver is not null) == true;
		_priceEditError = null;
		_showPriceEdit = true;
	}

	private void ClosePriceEdit() => _showPriceEdit = false;

	private async Task SubmitPriceEdit()
	{
		if (_editMaterialCost is null && _editCommission is null && _editDeliveryCost is null)
		{
			_priceEditError = "At least one of Material Cost / Commission / Deliveries Total must be set.";
			return;
		}

		if (_editMaterialCost < 0m || _editCommission < 0m || _editDeliveryCost < 0m)
		{
			_priceEditError = "Values cannot be negative.";
			return;
		}

		if (_editDeliveryCost is not null && _deliveryCount == 0)
		{
			_priceEditError = "Order has no deliveries, Deliveries Total cannot be applied.";
			return;
		}

		// Не гоняем на backend неизменённую сумму: иначе каждое открытие модалки писало бы
		// в аудит «правку доставки» и перезаписывало цены из-за округления при распределении.
		var deliveryCost = _editDeliveryCost == _originalDeliveryCost ? null : _editDeliveryCost;

		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Put($"admin/orders/{Id}/calculation", new SetOrderCalculationRequest(_editMaterialCost, _editCommission, deliveryCost));
				toastService.ShowSuccess("Order price updated (audit logged).");
				_showPriceEdit = false;
				// Кэш CalculationInfo (TTL 7 мин) — принудительно свежая выборка после правки.
				_allCalculationInfo = await cashService.GetData<CalculationInfo>(useCash: false);
				await LoadOrder();
			}
			catch (Exception ex)
			{
				_priceEditError = ex.Message;
			}
		});
	}

	private void OpenTimeEdit()
	{
		if (_order is null)
		{
			return;
		}

		_editPreferredDeliveryTime = _order.PreferredDeliveryTime;
		_timeEditError = null;
		_showTimeEdit = true;
	}

	private void CloseTimeEdit() => _showTimeEdit = false;

	private async Task SubmitTimeEdit()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Put($"admin/orders/{Id}/preferred-delivery-time", new SetPreferredDeliveryTimeRequest(_editPreferredDeliveryTime));
				toastService.ShowSuccess("Preferred delivery time updated.");
				_showTimeEdit = false;
				// Слоты водителей изменились на backend (restore+reserve) — обновляем все данные страницы.
				await FetchData();
				await LoadOrder();
			}
			catch (Exception ex)
			{
				// 409 от backend (нет слота у назначенного водителя) — текст приходит в теле ответа.
				_timeEditError = ex.Message;
			}
		});
	}

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
		// Independent cache keys load in parallel; slots and users then come from the cache filled here.
		var calculationInfo = cashService.GetData<CalculationInfo>().AsTask();
		var regions = cashService.GetData<Region>().AsTask();
		var driversWithCompany = cashService.GetDriverWithCompany().AsTask();
		var vehicles = cashService.GetData<Vehicle>().AsTask();
		await Task.WhenAll(calculationInfo, regions, driversWithCompany, vehicles);

		_allCalculationInfo = calculationInfo.Result;
		_levels = [.. regions.Result.SelectMany(r => r.Levels)];
		_driversWithCompany = driversWithCompany.Result;
		_driverSlots = await cashService.GetSlots();
		var user = await cashService.GetData<User>();
		_vehicles = vehicles.Result.MapDriversToVehicles(_driversWithCompany, user);
	}

	private void RequestResetPayments()
	{
		_confirm.Ask("Reset Payments", "Are you sure you want to reset all payments for this order? This action cannot be undone.", ResetPayments);
	}

	private async Task ResetPayments()
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/payments/reset");
			toastService.ShowSuccess("Payments reset successfully");
			await LoadOrder(); // Refresh
		}, "Error resetting payments: ");
	}

	private void RequestAcceptDelivery(double weight)
	{
		_selectedDriverIds.TryGetValue(weight, out var selectedDriverId);

		var companyId = _driversWithCompany.CompanyOf(selectedDriverId)?.Id ?? Guid.Empty;
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

		_confirm.Ask("Accept Delivery", $"Are you sure you want to assign this delivery with weight {weight} to the selected driver?", async () => await AcceptDelivery(weight, companyId, selectedDriverId));
	}

	private async Task AcceptDelivery(double weight, Guid companyId, Guid driverId)
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/accept?companyId={companyId}&weight={weight}&driverId={driverId}");
			toastService.ShowSuccess("Delivery accepted successfully");
			_selectedDriverIds.Remove(weight);
			await LoadOrder();
		}, "Error accepting delivery: ");
	}

	private void RequestUpdateStatus()
	{
		_confirm.Ask("Update Status", $"Are you sure you want to update the status to {_newStatus}?", UpdateStatus);
	}

	private async Task UpdateStatus()
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/status?status={_newStatus}&state={Uri.EscapeDataString(_newState)}");
			toastService.ShowSuccess("Status updated successfully");
			await LoadOrder();
		}, "Error updating status: ");
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
			OrderStatus.Weighted => "Заказ взвешен ⚖️ осталось довезти и завершить 🚛",
			OrderStatus.Completed => "Считаем наши денюзки 🤑, не забываем оплатить Перевозчикам/Поставщикам",
			OrderStatus.CorruptedPayment => "💀 надо чтото сделать с этим ⏰",
			OrderStatus.Cancelled => "Увы но отмена 😢",
			OrderStatus.Archived => "Уже и не вспомнить что с ним было 😅",
			OrderStatus.Deleted => "👷 уже нет",
			OrderStatus.PaymentInProgress => "📣 обрабатываем платёж надеюсь всё ок 🤞 статус поменяется быстро",
			_ => "There is no state 🗽"
		};
	}

	private void ShowUserDetails()
	{
		_userDetailsOpen = !_userDetailsOpen;
		_userDetailsButtonName = _userDetailsOpen ? "Hide Details" : "Show Details";
	}

	private void SetDriverToView(Guid? driverId)
	{
		if (driverId == null)
		{
			_driverInfo = null;
		}

		var driverWithCompany = _driversWithCompany.FirstOrDefault(dc => dc.Item2.Id == driverId);
		if (driverWithCompany != default)
		{
			_driverInfo = driverWithCompany.Item2;
			_driverInfo.UserInfo?.Company = driverWithCompany.Item1;
		}
	}

	private (Company comapny, Driver driver, bool hasSlot)[] GetDrivers(double duration, uint weight, DateTime deliveryTime)
	{
		// Called from markup for every delivery group on every render: set and lookup instead of nested scans.
		var acceptableDrivers = _vehicles.Where(v => v.LoadCapacity == weight).Select(v => v.Driver!.Id).ToHashSet();
		var slotsByDriver = _driverSlots.Where(slot => acceptableDrivers.Contains(slot.DriverId)).ToLookup(slot => slot.DriverId);

		return [.. _driversWithCompany
			.Where(driverWithCompany => acceptableDrivers.Contains(driverWithCompany.Item2.Id))
			.Select(driverWithCompany => (driverWithCompany.Item1, driverWithCompany.Item2, slotsByDriver[driverWithCompany.Item2.Id].ToArray().HasSlot(duration, deliveryTime)))];
	}

	// Компания водителя для печатных деталей (связь живёт в _driversWithCompany, а не в UserInfo)
	private Company? GetDriverCompany(Guid driverId) => _driversWithCompany.CompanyOf(driverId);

	private static string PrintKey(string key)
	{
		return key.FormatJsonKey();
	}

}
