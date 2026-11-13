using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Helpers;
using WebWasm.Localization;
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
	private SlotTimeParams _slotTimeParams = SlotTimeParams.Default;
	private static readonly OrderStatus[] _allStatuses = Enum.GetValues<OrderStatus>();
	private (Company, Driver)[] _driversWithCompany = [];
	private readonly Dictionary<double, Guid> _selectedDriverIds = [];
	private bool _userDetailsOpen;

	private Driver? _driverInfo;

	// For Status Change
	private OrderStatus _newStatus;
	private string _newState = string.Empty;

	// Confirmation Dialog: the action verb and its danger differ per question.
	private readonly ConfirmState _confirm = new();
	private string? _confirmText;
	private bool _confirmDangerous = true;

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
	// Превью с сервера: та же функция, что при сохранении, поэтому SA видит те цифры, которые запишутся.
	private CalculationInfo? _pricePreview;

	private bool _showTimeEdit;
	private DateTime _editPreferredDeliveryTime;
	private string? _timeEditError;

	private string TabTitle => _order is null ? T.Orders.Order : T.Orders.OrderTitle(_order.Name);

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
		_pricePreview = _calculationInfo;
		_showPriceEdit = true;
	}

	private void ClosePriceEdit() => _showPriceEdit = false;

	private SetOrderCalculationRequest PriceEditRequest() =>
		// Не гоняем на backend неизменённую сумму доставок: иначе каждое открытие модалки писало бы
		// в аудит «правку доставки» и перезаписывало цены из-за округления при распределении.
		new(_editMaterialCost, _editCommission, _editDeliveryCost == _originalDeliveryCost ? null : _editDeliveryCost);

	private async Task RefreshPricePreview()
	{
		if (_editMaterialCost < 0m || _editCommission < 0m || _editDeliveryCost < 0m)
		{
			_priceEditError = T.Orders.NegativeValues;
			return;
		}

		try
		{
			_pricePreview = await apiClient.Post<SetOrderCalculationRequest, CalculationInfo>($"admin/orders/{Id}/calculation/preview", PriceEditRequest());
			_priceEditError = null;
		}
		catch (Exception ex)
		{
			_priceEditError = T.Orders.PreviewFailed(ex.Message);
		}
	}

	private async Task SubmitPriceEdit()
	{
		if (_editMaterialCost is null && _editCommission is null && _editDeliveryCost is null)
		{
			_priceEditError = T.Orders.NothingToSave;
			return;
		}

		if (_editMaterialCost < 0m || _editCommission < 0m || _editDeliveryCost < 0m)
		{
			_priceEditError = T.Orders.NegativeValues;
			return;
		}

		if (_editDeliveryCost is not null && _deliveryCount == 0)
		{
			_priceEditError = T.Orders.NoDeliveriesToApply;
			return;
		}

		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Put($"admin/orders/{Id}/calculation", PriceEditRequest());
				toastService.ShowSuccess(T.Orders.PriceUpdated);
				_showPriceEdit = false;
				cashService.Invalidate<Order>();
				// Кэш CalculationInfo (TTL 7 мин) — принудительно свежая выборка после правки.
				_allCalculationInfo = await cashService.GetData<CalculationInfo>(useCash: false);
				await LoadOrder();
			}
			catch (Exception ex)
			{
				_priceEditError = T.Orders.PriceSaveFailed(ex.Message);
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
				toastService.ShowSuccess(T.Orders.TimeUpdated);
				_showTimeEdit = false;
				// Слоты водителей изменились на backend (restore+reserve): их кэш устарел.
				cashService.Invalidate<Order>();
				await RefreshSlots();
				await LoadOrder();
			}
			catch (Exception ex)
			{
				// 409 от backend (нет слота у назначенного водителя) — текст приходит в теле ответа.
				_timeEditError = T.Orders.TimeSaveFailed(ex.Message);
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
					toastService.ShowError(T.Orders.NotFound);
					return;
				}

				_calculationInfo = _allCalculationInfo.FirstOrDefault(c => c.Id == Id);
				_newStatus = _order.Status;
				_newState = _order.State ?? string.Empty;
			}
			catch (Exception ex)
			{
				toastService.ShowError(T.Orders.LoadFailed(ex.Message));
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
		var slotTimeParams = LoadSlotTimeParams();
		await Task.WhenAll(calculationInfo, regions, driversWithCompany, vehicles, slotTimeParams);

		_slotTimeParams = slotTimeParams.Result;

		_allCalculationInfo = calculationInfo.Result;
		_levels = [.. regions.Result.SelectMany(r => r.Levels)];
		_driversWithCompany = driversWithCompany.Result;
		_driverSlots = await cashService.GetSlots();
		var user = await cashService.GetData<User>();
		_vehicles = vehicles.Result.MapDriversToVehicles(_driversWithCompany, user);
	}

	/// <summary>
	/// Слот водителя считается по тем же глобальным настройкам, что backend применяет на AcceptOrder.
	/// Настройки не загрузились — дефолты и тост: страница заказа важнее точности подсказки о слоте.
	/// </summary>
	private async Task<SlotTimeParams> LoadSlotTimeParams()
	{
		try
		{
			return SlotTimeParams.From(await apiClient.Get<PricingSettingItem[]>("admin/pricing-settings?scope=Global"));
		}
		catch (Exception ex)
		{
			toastService.ShowError(T.Orders.SlotSettingsFailed(ex.Message));
			return SlotTimeParams.Default;
		}
	}

	private async Task RefreshSlots()
	{
		var (users, companies, drivers) = await cashService.GetUsersCompaniesDrivers();
		_driverSlots = await cashService.GetSlots(drivers, users, companies, useCash: false);
	}

	private void Ask(string title, string message, string confirmText, bool dangerous, Func<Task> action)
	{
		_confirmText = confirmText;
		_confirmDangerous = dangerous;
		_confirm.Ask(title, message, action);
	}

	private void RequestResetPayments()
	{
		Ask(T.Orders.ResetPaymentsTitle, T.Orders.ResetPaymentsMessage, T.Orders.ResetPayments, dangerous: true, ResetPayments);
	}

	private async Task ResetPayments()
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/payments/reset");
			toastService.ShowSuccess(T.Orders.PaymentsReset);
			cashService.Invalidate<Order>();
			await LoadOrder(); // Refresh
		}, T.Orders.ResetPaymentsFailed);
	}

	private void RequestAcceptDelivery(double weight)
	{
		_selectedDriverIds.TryGetValue(weight, out var selectedDriverId);

		var companyId = _driversWithCompany.CompanyOf(selectedDriverId)?.Id ?? Guid.Empty;
		if (selectedDriverId == Guid.Empty)
		{
			toastService.ShowError(T.Orders.SelectDriver);
			return;
		}

		if (companyId == Guid.Empty)
		{
			toastService.ShowError(T.Orders.DriverWithoutCompany);
			return;
		}

		Ask(T.Orders.AssignTitle, T.Orders.AssignMessage(FormatWeight(weight)), T.Orders.Assign, dangerous: false,
			async () => await AcceptDelivery(weight, companyId, selectedDriverId));
	}

	private async Task AcceptDelivery(double weight, Guid companyId, Guid driverId)
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/accept?companyId={companyId}&weight={weight}&driverId={driverId}");
			toastService.ShowSuccess(T.Orders.DriverAssigned);
			_selectedDriverIds.Remove(weight);
			// Принятие вырезает окно из слотов водителя и меняет цену заказа (цена перевозчика).
			cashService.Invalidate<Order>();
			_allCalculationInfo = await cashService.GetData<CalculationInfo>(useCash: false);
			await RefreshSlots();
			await LoadOrder();
		}, T.Orders.AssignFailed);
	}

	private void RequestUpdateStatus()
	{
		Ask(T.Orders.UpdateStatusTitle, T.Orders.UpdateStatusMessage(T.Enums.OrderStatus(_newStatus)), T.Orders.UpdateStatus, dangerous: false, UpdateStatus);
	}

	private async Task UpdateStatus()
	{
		await loadingService.Run(toastService, async () =>
		{
			await apiClient.Post($"Orders/{Id}/status?status={_newStatus}&state={Uri.EscapeDataString(_newState)}");
			toastService.ShowSuccess(T.Orders.StatusUpdated);
			cashService.Invalidate<Order>();
			await LoadOrder();
		}, T.Orders.StatusUpdateFailed);
	}

	/// <summary>What the current status means for the admin; urgent when the order needs action right now.</summary>
	private (string Text, bool Urgent) StatusHint()
	{
		if (_order is null)
		{
			return (string.Empty, false);
		}

		var deliveryToday = _order.PreferredDeliveryTime.Date == DateTime.Now.Date;
		var waitingLong = _order.Created < DateTime.Now.AddHours(-2);
		var urgent = _order.Status switch
		{
			OrderStatus.WaitingApprove or OrderStatus.PaymentPending => deliveryToday,
			OrderStatus.CorruptedPayment => true,
			_ => false,
		};

		return (T.Orders.StatusHint(_order.Status, deliveryToday, waitingLong), urgent);
	}

	private void ToggleUserDetails() => _userDetailsOpen = !_userDetailsOpen;

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
			.Select(driverWithCompany => (driverWithCompany.Item1, driverWithCompany.Item2, slotsByDriver[driverWithCompany.Item2.Id].ToArray().HasSlot(duration, deliveryTime, _slotTimeParams)))];
	}

	// Компания водителя для печатных деталей (связь живёт в _driversWithCompany, а не в UserInfo)
	private Company? GetDriverCompany(Guid driverId) => _driversWithCompany.CompanyOf(driverId);

	private static string PrintKey(string key)
	{
		return key.FormatJsonKey();
	}

	/// <summary>Weight as it comes from the API; decimals only when the value has them.</summary>
	private string FormatWeight(double value) => T.FormatNumber(value, value % 1 == 0 ? 0 : 2);

	/// <summary>Delivery weight in kilograms shown in tonnes: 20 т, 7,5 т.</summary>
	private string Tonnes(uint kilograms) => T.Orders.Tonnes(Trimmed(kilograms / 1000m));

	// D-po-21/25: VAT of material + deliveries + commission; null parts = order created before the split.
	private static decimal TotalVat(CalculationInfo info) =>
		(info.MaterialVatAmount ?? 0) + info.DeliveryInfo.Sum(x => x.VatAmount) + (info.CommissionVatAmount ?? 0);

	private string Percent(decimal value) => T.FormatPercent(value, Decimals(value));

	private string Trimmed(decimal value) => T.FormatNumber(value, Decimals(value));

	private static int Decimals(decimal value) => value % 1 == 0 ? 0 : value * 10 % 1 == 0 ? 1 : 2;

	private static string FullName(UserInfo? userInfo) =>
		OrDash(userInfo is null ? null : string.Join(' ', new[] { userInfo.FirstName, userInfo.MiddleName, userInfo.LastName }.Where(n => !string.IsNullOrWhiteSpace(n))));

	private static string OrDash(string? value) => string.IsNullOrWhiteSpace(value) ? Texts.Dash : value;
}
