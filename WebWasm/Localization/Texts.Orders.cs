using WebWasm.Models;

namespace WebWasm.Localization;

public sealed partial class Texts
{
	public OrdersTexts Orders => field ??= new(Language);
}

/// <summary>Orders list and OrderDetails: management, price and delivery time edits, deliveries, transactions.</summary>
public sealed class OrdersTexts(Language language) : TextBase(language)
{
	// Orders list
	public string Subtitle => T("Заказы покупателей: статусы, суммы и доставка", "Buyer orders: statuses, amounts and delivery");
	public string StatTotal => T("Всего заказов", "Total orders");
	public string StatPending => T("Ждут подтверждения или оплаты", "Waiting for approval or payment");
	public string StatCompleted => T("Выполнено", "Completed");
	public string SearchPlaceholder => T("Поиск по названию, адресу и сообщению статуса", "Search by name, address and status message");
	public string Filters => T("Фильтры", "Filters");
	public string Weight => T("Вес", "Weight");
	public string WeightFrom => T("от", "from");
	public string WeightTo => T("до", "to");
	public string MinWeight => T("Минимальный вес", "Minimum weight");
	public string MaxWeight => T("Максимальный вес", "Maximum weight");
	public string ResetWeight => T("Сбросить фильтр по весу", "Reset the weight filter");
	public string Statuses => T("Статусы", "Statuses");
	public string SelectStatus => T("Выберите статус…", "Select a status…");
	public string RemoveStatus(string status) => T($"Убрать статус «{status}» из фильтра", $"Remove «{status}» from the filter");
	public string CreatedDate => T("Дата создания", "Created date");
	public string ResetDates => T("Сбросить фильтр по дате", "Reset the date filter");
	public string Cost => T("Стоимость", "Cost");
	public string DeliveryTime => T("Время доставки", "Delivery time");
	public string Empty => T("Заказов пока нет", "No orders yet");
	public string EmptyHint => T("Заказы появятся, когда покупатели оформят их в приложении.", "Orders appear when buyers place them in the app.");

	// Order details: header and management
	public string Order => T("Заказ", "Order");
	public string OrderTitle(string name) => T($"Заказ {name}", $"Order {name}");
	public string NotFound => T("Заказ не найден. Проверьте ссылку или откройте его из списка заказов.", "Order not found. Check the link or open it from the order list.");
	public string LoadFailed(string reason) => T($"Не удалось загрузить заказ: {reason}", $"Could not load the order: {reason}");
	public string SlotSettingsFailed(string reason) => T(
		$"Не удалось загрузить настройки слотов, подсказки о слотах водителей считаются по значениям по умолчанию: {reason}",
		$"Could not load the slot settings, driver slot hints use the defaults: {reason}");
	public string Management => T("Управление", "Management");
	public string NewStatus => T("Новый статус", "New status");
	public string StateMessage => T("Сообщение статуса", "Status message");
	public string Optional => T("Необязательно", "Optional");
	public string UpdateStatus => T("Изменить статус", "Change status");
	public string UpdateStatusTitle => T("Изменить статус заказа?", "Change the order status?");
	public string UpdateStatusMessage(string status) => T($"Заказ получит статус «{status}».", $"The order will get the status «{status}».");
	public string StatusUpdated => T("Статус заказа изменён", "Order status changed");
	public string StatusUpdateFailed(string reason) => T($"Не удалось изменить статус: {reason}", $"Could not change the status: {reason}");
	public string ResetPayments => T("Сбросить платежи", "Reset payments");
	public string ResetPaymentsTitle => T("Сбросить платежи заказа?", "Reset the order payments?");
	public string ResetPaymentsMessage => T("Все платежи по заказу будут сброшены. Действие нельзя отменить.", "All payments of the order will be reset. This cannot be undone.");
	public string PaymentsReset => T("Платежи сброшены", "Payments reset");
	public string ResetPaymentsFailed(string reason) => T($"Не удалось сбросить платежи: {reason}", $"Could not reset the payments: {reason}");

	/// <summary>What the status means for the admin and what to do next.</summary>
	public string StatusHint(OrderStatus status, bool deliveryToday, bool waitingLong) => status switch
	{
		OrderStatus.Draft => T("Черновик: покупатель не завершил оформление заказа.", "Draft: the buyer has not finished placing the order."),
		OrderStatus.WaitingApprove when deliveryToday => T(
			"Доставка сегодня, а заказ ещё не принял ни один перевозчик. Назначьте водителя или отмените заказ.",
			"Delivery is today, but no carrier has accepted the order. Assign a driver or cancel the order."),
		OrderStatus.WaitingApprove when waitingLong => T(
			"Заказ ждёт перевозчика больше 2 часов. Проверьте, есть ли свободные водители.",
			"The order has been waiting for a carrier for more than 2 hours. Check for free drivers."),
		OrderStatus.WaitingApprove => T("Новый заказ: ждёт, когда его примет перевозчик.", "New order: waiting for a carrier to accept it."),
		OrderStatus.PaymentPending when deliveryToday => T(
			"Доставка сегодня, а заказ ещё не оплачен. Свяжитесь с покупателем или отмените заказ.",
			"Delivery is today, but the order is not paid. Contact the buyer or cancel the order."),
		OrderStatus.PaymentPending => T("Заказ ждёт оплаты от покупателя.", "Waiting for the buyer to pay."),
		OrderStatus.Active when deliveryToday => T("Доставка по заказу сегодня.", "The delivery is today."),
		OrderStatus.Active => T("Активный заказ: ждёт дня доставки.", "Active order: waiting for the delivery day."),
		OrderStatus.Weighted => T("Груз взвешен: осталось довезти его и завершить заказ.", "The cargo is weighed: deliver it and complete the order."),
		OrderStatus.Completed => T("Заказ выполнен. Проверьте выплаты перевозчикам и поставщику.", "Completed. Check the payouts to the carriers and the supplier."),
		OrderStatus.CorruptedPayment => T(
			"Ошибка оплаты: проверьте транзакции ниже, затем сбросьте платежи или смените статус.",
			"Payment error: check the transactions below, then reset the payments or change the status."),
		OrderStatus.Cancelled => T("Заказ отменён.", "The order is cancelled."),
		OrderStatus.Archived => T("Заказ в архиве.", "The order is archived."),
		OrderStatus.Deleted => T("Заказ удалён.", "The order is deleted."),
		OrderStatus.PaymentInProgress => T("Платёж обрабатывается, статус сменится автоматически.", "The payment is being processed, the status will change automatically."),
		_ => T("Для этого статуса нет подсказки.", "No hint for this status."),
	};

	// Order details: information
	public string OrderInfo => T("Данные заказа", "Order details");
	public string Duration => T("Длительность", "Duration");
	public string DurationValue(TimeSpan duration)
	{
		var minutes = (int)duration.TotalMinutes;
		return T($"{minutes / 60} ч {minutes % 60:00} мин", $"{minutes / 60} h {minutes % 60:00} min");
	}
	public string TotalWeight => T("Общий вес", "Total weight");
	public string Distance => T("Расстояние", "Distance");
	public string Km(string value) => T($"{value} км", $"{value} km");
	public string Tonnes(string value) => T($"{value} т", $"{value} t");
	public string RegionLevel => T("Уровень региона", "Region level");
	public string LoadingPlace => T("Площадка погрузки", "Loading place");
	public string Buyer => T("Покупатель", "Buyer");
	public string BuyerDetails => T("Данные покупателя", "Buyer details");
	public string ShowDetails => T("Показать данные", "Show details");
	public string HideDetails => T("Скрыть данные", "Hide details");
	public string EditDeliveryTime => T("Изменить время доставки", "Change the delivery time");
	public string Location => T("Местоположение", "Location");
	public string DeliveryPoint => T("Точка доставки", "Delivery point");
	public string Coordinates(string latitude, string longitude) => T($"Широта {latitude}, долгота {longitude}", $"Latitude {latitude}, longitude {longitude}");
	public string Material => T("Материал", "Material");
	public string NoMaterial => T("Материал не указан", "No material");
	public string Supplier => T("Поставщик", "Supplier");
	public string SupplierCompany => T("Компания поставщика", "Supplier company");
	public string NoSupplier => T("Поставщик не указан", "No supplier");
	public string LegalForm => T("Правовая форма", "Legal form");
	public string BankAccount => T("Расчётный счёт и БИК", "Bank account and BIC");
	public string Unp => T("УНП", "UNP");
	public string Producer => T("Производитель", "Producer");
	public string MaterialPrice => T("Цена материала", "Material price");
	public string Volume => T("Объём", "Volume");
	public string CompanyAddress => T("Адрес компании", "Company address");

	// Order details: cost calculation and price edit
	public string Calculation => T("Расчёт стоимости", "Cost calculation");
	public string EditPrice => T("Изменить цену", "Edit price");
	public string TotalCost => T("Итоговая стоимость", "Total cost");
	public string DeliveriesTotal => T("Доставки, итого", "Deliveries total");
	public string TotalVat => T("НДС, итого", "Total VAT");
	public string Commission => T("Комиссия", "Commission");
	public string Component => T("Составляющая", "Component");
	public string Amount => T("Сумма", "Amount");
	public string Net => T("Нетто", "Net");
	public string Vat => T("НДС", "VAT");
	public string VatRate => T("Ставка НДС", "VAT rate");
	public string Fee => T("Сбор", "Fee");
	public string DeliveryOf(string weight) => T($"Доставка {weight}", $"Delivery {weight}");
	public string PriceEditTitle => T("Изменить цену заказа", "Edit the order price");
	public string MaterialCost => T("Стоимость материала, BYN", "Material cost, BYN");
	public string CommissionBYN => T("Комиссия, BYN", "Commission, BYN");
	public string DeliveriesTotalBYN => T("Доставки итого, BYN с НДС", "Deliveries total, BYN incl. VAT");
	public string NoDeliveriesHint => T("У заказа нет доставок: стоимость доставки менять нельзя.", "The order has no deliveries: the delivery cost cannot be changed.");
	public string SplitHint(int count) => T(
		$"Сумма распределится между доставками ({count}) пропорционально их текущим ценам.",
		$"The sum is split between the {count} deliveries in proportion to their current prices.");
	public string PriceEditNote => T(
		"Все суммы — итоги с НДС и сбором. Нетто и НДС сервер пересчитает по формуле заказа; в таблице ниже — ровно то, что сохранится. Каждая правка записывается в журнал аудита.",
		"All sums include VAT and the fee. The server recalculates net and VAT by the order formula; the table below shows exactly what will be saved. Every edit is written to the audit log.");
	public string AcceptedDeliveryWarning => T(
		"По заказу уже есть принятые доставки: правка стоимости доставки изменит вознаграждение перевозчика.",
		"Some deliveries are already accepted: changing the delivery cost changes the carrier's pay.");
	public string NegativeValues => T("Суммы не могут быть отрицательными.", "Sums cannot be negative.");
	public string NothingToSave => T("Укажите стоимость материала, комиссию или сумму доставок.", "Set the material cost, the commission or the deliveries total.");
	public string NoDeliveriesToApply => T("У заказа нет доставок: сумму доставок применить нельзя.", "The order has no deliveries: the deliveries total cannot be applied.");
	public string PreviewFailed(string reason) => T($"Не удалось пересчитать цену: {reason}", $"Could not recalculate the price: {reason}");
	public string PriceSaveFailed(string reason) => T($"Не удалось сохранить цену: {reason}", $"Could not save the price: {reason}");
	public string PriceUpdated => T("Цена заказа изменена, правка записана в журнал аудита", "Order price changed and written to the audit log");

	// Order details: delivery time edit
	public string DeliveryTimeLocal => T("Время доставки, местное время региона", "Delivery time, region local time");
	public string TimeEditNote => T(
		"Старое время вернётся в слоты назначенных водителей, новое будет зарезервировано. Если у водителя нет свободного слота, сервер откажет в изменении.",
		"The old time returns to the assigned drivers' slots, the new one is reserved. If a driver has no free slot, the server rejects the change.");
	public string TimeUpdated => T("Время доставки изменено", "Delivery time changed");
	public string TimeSaveFailed(string reason) => T($"Не удалось изменить время доставки: {reason}", $"Could not change the delivery time: {reason}");

	// Order details: deliveries and drivers
	public string Deliveries => T("Доставки", "Deliveries");
	public string NoDeliveries => T("Доставок нет", "No deliveries");
	public string WeightGroup(string weight) => T($"Вес {weight}", $"Weight {weight}");
	public string DeliveryState => T("Состояние", "State");
	public string BatchNumber => T("Номер партии", "Batch number");
	public string Weights => T("Вес: нетто / брутто / учтённый", "Weight: net / gross / applied");
	public string Driver => T("Водитель", "Driver");
	public string DriverDetails => T("Данные водителя", "Driver details");
	public string DriverDetailsOf(string name) => T($"Данные водителя: {name}", $"Driver details: {name}");
	public string AssignDriverFor(string weight) => T($"Водитель для свободной доставки весом {weight}", $"Driver for a free delivery of {weight}");
	public string SelectDriverOption => T("Выберите водителя…", "Select a driver…");
	public string NoName => T("Без имени", "No name");
	public string NoSlot => T("нет свободного слота", "no free slot");
	public string Assign => T("Назначить", "Assign");
	public string SelectDriver => T("Выберите водителя.", "Select a driver.");
	public string DriverWithoutCompany => T("У водителя нет компании, назначить его нельзя.", "The driver has no company and cannot be assigned.");
	public string AssignTitle => T("Назначить водителя?", "Assign the driver?");
	public string AssignMessage(string weight) => T($"Доставка весом {weight} перейдёт выбранному водителю.", $"The delivery of {weight} goes to the selected driver.");
	public string DriverAssigned => T("Водитель назначен", "Driver assigned");
	public string AssignFailed(string reason) => T($"Не удалось назначить водителя: {reason}", $"Could not assign the driver: {reason}");

	// Order details: transactions
	public string Transactions => T("Транзакции", "Transactions");
	public string NoTransactions => T("Транзакций нет", "No transactions");
	public string PaymentMethod => T("Способ оплаты", "Payment method");
}
