using WebWasm.Models;

namespace WebWasm.Localization;

public sealed partial class Texts
{
	public CommonTexts Common => field ??= new(Language);
	public EnumTexts Enums => field ??= new(Language);
}

/// <summary>Words used on many screens. A word used by one screen lives in that screen's area.</summary>
public sealed class CommonTexts(Language language) : TextBase(language)
{
	public string Save => T("Сохранить", "Save");
	public string Cancel => T("Отмена", "Cancel");
	public string Delete => T("Удалить", "Delete");
	public string Edit => T("Изменить", "Edit");
	public string Add => T("Добавить", "Add");
	public string Create => T("Создать", "Create");
	public string Close => T("Закрыть", "Close");
	public string Confirm => T("Подтвердить", "Confirm");
	public string Apply => T("Применить", "Apply");
	public string Reset => T("Сбросить", "Reset");
	public string Clear => T("Очистить", "Clear");
	public string Back => T("Назад", "Back");
	public string Refresh => T("Обновить", "Refresh");
	public string Copy => T("Копировать", "Copy");
	public string Copied => T("Скопировано", "Copied");
	public string CopyFailed => T("Не удалось скопировать: браузер не дал доступ к буферу обмена", "Could not copy: the browser denied clipboard access");
	public string Details => T("Подробнее", "Details");
	public string Actions => T("Действия", "Actions");
	public string Expand => T("Развернуть", "Expand");
	public string Collapse => T("Свернуть", "Collapse");
	public string ShowMore => T("Показать ещё", "Show more");
	public string Yes => T("Да", "Yes");
	public string No => T("Нет", "No");
	public string All => T("Все", "All");
	public string Any => T("Любой", "Any");
	public string None => T("Нет", "None");
	public string NotSet => T("Не задано", "Not set");
	public string Loading => T("Загрузка…", "Loading…");
	public string Search => T("Поиск", "Search");
	public string SearchPlaceholder => T("Поиск…", "Search…");
	public string Total => T("Итого", "Total");
	public string From => T("с", "from");
	public string To => T("по", "to");
	public string Required => T("Обязательное поле", "Required");

	public string Name => T("Название", "Name");
	public string Description => T("Описание", "Description");
	public string Status => T("Статус", "Status");
	public string Type => T("Тип", "Type");
	public string Created => T("Создан", "Created");
	public string Updated => T("Изменён", "Updated");
	public string Date => T("Дата", "Date");
	public string Phone => T("Телефон", "Phone");
	public string Email => T("Эл. почта", "Email");
	public string Company => T("Компания", "Company");
	public string Region => T("Регион", "Region");
	public string Photo => T("Фото", "Photo");
	public string Address => T("Адрес", "Address");
	public string Active => T("Активен", "Active");
	public string Inactive => T("Не активен", "Inactive");

	public string NothingFound => T("Ничего не найдено", "Nothing found");
	public string NothingFoundHint => T("Измените поиск или фильтры.", "Change the search or the filters.");
	public string NoData => T("Пока пусто", "Nothing here yet");

	public string ConfirmDeleteTitle => T("Удалить?", "Delete?");
	public string CannotBeUndone => T("Действие нельзя отменить.", "This cannot be undone.");
	public string Saved => T("Сохранено", "Saved");
	public string Deleted => T("Удалено", "Deleted");

	/// <summary>Toast after a failed action: «Не удалось сохранить: причина».</summary>
	public string Failed(string action, string reason) => T($"Не удалось {action}: {reason}", $"Could not {action}: {reason}");

	public string LoadFailed(string reason) => T($"Не удалось загрузить данные: {reason}", $"Could not load data: {reason}");

	// Reasons of a failed request (ApiClient): they follow the action prefix of the toast.
	public string SessionExpired => T("сессия истекла, войдите снова", "the session has expired, sign in again");
	public string ServerError(int code, string details) => string.IsNullOrWhiteSpace(details)
		? T($"сервер ответил кодом {code}", $"the server answered with code {code}")
		: T($"сервер ответил кодом {code}: {details}", $"the server answered with code {code}: {details}");
	public string EmptyResponse => T("сервер вернул пустой ответ", "the server returned an empty response");

	public string PageOf(int page, int pages) => T($"Страница {page} из {pages}", $"Page {page} of {pages}");
	public string FirstPage => T("Первая страница", "First page");
	public string PreviousPage => T("Предыдущая страница", "Previous page");
	public string NextPage => T("Следующая страница", "Next page");
	public string LastPage => T("Последняя страница", "Last page");

	public string Count(int count) => T($"{count} шт.", $"{count}");
}

/// <summary>Names of enum values from Models (backend contracts).</summary>
public sealed class EnumTexts(Language language) : TextBase(language)
{
	public string OrderStatus(OrderStatus status) => status switch
	{
		Models.OrderStatus.Draft => T("Черновик", "Draft"),
		Models.OrderStatus.WaitingApprove => T("Ждёт подтверждения", "Waiting for approval"),
		Models.OrderStatus.PaymentPending => T("Ждёт оплаты", "Payment pending"),
		Models.OrderStatus.Active => T("Активный", "Active"),
		Models.OrderStatus.Completed => T("Выполнен", "Completed"),
		Models.OrderStatus.CorruptedPayment => T("Ошибка оплаты", "Payment error"),
		Models.OrderStatus.Cancelled => T("Отменён", "Cancelled"),
		Models.OrderStatus.Archived => T("В архиве", "Archived"),
		Models.OrderStatus.Deleted => T("Удалён", "Deleted"),
		Models.OrderStatus.PaymentInProgress => T("Идёт оплата", "Payment in progress"),
		Models.OrderStatus.Weighted => T("Взвешен", "Weighed"),
		_ => status.ToString(),
	};

	public string Role(RoleType role) => role switch
	{
		RoleType.Driver => T("Водитель", "Driver"),
		RoleType.TransportManager => T("Менеджер перевозок", "Transport manager"),
		RoleType.OperatorProducer => T("Оператор поставщика", "Supplier operator"),
		RoleType.AccountAdmin => T("Администратор компании", "Company admin"),
		RoleType.SuperAdmin => T("Суперадмин", "Super admin"),
		_ => role.ToString(),
	};

	/// <summary>Company type is a set of flags: «Покупатель, Перевозчик».</summary>
	public string CompanyType(CompanyType type)
	{
		if (type == Models.CompanyType.None)
		{
			return T("Без типа", "No type");
		}

		var names = new List<string>(3);
		if (type.HasFlag(Models.CompanyType.Buyer))
		{
			names.Add(CompanyRole(Models.CompanyType.Buyer));
		}

		if (type.HasFlag(Models.CompanyType.Cargo))
		{
			names.Add(CompanyRole(Models.CompanyType.Cargo));
		}

		if (type.HasFlag(Models.CompanyType.Provider))
		{
			names.Add(CompanyRole(Models.CompanyType.Provider));
		}

		return string.Join(", ", names);
	}

	/// <summary>One flag of <see cref="Models.CompanyType"/>.</summary>
	public string CompanyRole(CompanyType flag) => flag switch
	{
		Models.CompanyType.Buyer => T("Покупатель", "Buyer"),
		Models.CompanyType.Cargo => T("Перевозчик", "Carrier"),
		Models.CompanyType.Provider => T("Поставщик", "Supplier"),
		_ => CompanyType(flag),
	};

	public string SecurityLevel(DataSecurityLevel level) => level switch
	{
		DataSecurityLevel.Public => T("Открытые", "Public"),
		DataSecurityLevel.Internal => T("Внутренние", "Internal"),
		DataSecurityLevel.CompanyOperational => T("Рабочие данные компании", "Company operational"),
		DataSecurityLevel.Restricted => T("Ограниченные", "Restricted"),
		DataSecurityLevel.Sensitive => T("Конфиденциальные", "Sensitive"),
		DataSecurityLevel.SystemOnly => T("Только система", "System only"),
		_ => level.ToString(),
	};

	public string LevelType(LevelType type) => type switch
	{
		Models.LevelType.City => T("Город", "City"),
		Models.LevelType.Neighborhood => T("Пригород", "Suburb"),
		_ => type.ToString(),
	};

	public string PaymentMethod(PaymentMethodType method) => method switch
	{
		PaymentMethodType.None => T("Нет", "None"),
		PaymentMethodType.Card => T("Карта", "Card"),
		PaymentMethodType.BindCard => T("Привязка карты", "Card binding"),
		PaymentMethodType.TokenizedCard => T("Сохранённая карта", "Saved card"),
		_ => method.ToString(),
	};

	public string PaymentTransaction(PaymentTransactionType type) => type switch
	{
		PaymentTransactionType.None => T("Нет", "None"),
		PaymentTransactionType.Completed => T("Проведён", "Completed"),
		PaymentTransactionType.Declined => T("Отклонён", "Declined"),
		PaymentTransactionType.Authorized => T("Авторизован", "Authorized"),
		PaymentTransactionType.PartialRefunded => T("Частичный возврат", "Partially refunded"),
		PaymentTransactionType.Voided => T("Аннулирован", "Voided"),
		PaymentTransactionType.Failed => T("Ошибка", "Failed"),
		PaymentTransactionType.PartialVoided => T("Частично аннулирован", "Partially voided"),
		PaymentTransactionType.Recurrent => T("Рекуррентный", "Recurrent"),
		PaymentTransactionType.Refunded => T("Возвращён", "Refunded"),
		PaymentTransactionType.Blocked => T("Заблокирован", "Blocked"),
		PaymentTransactionType.Verification => T("Проверка", "Verification"),
		PaymentTransactionType.Pending => T("В ожидании", "Pending"),
		PaymentTransactionType.Cancelled => T("Отменён", "Cancelled"),
		_ => type.ToString(),
	};

	public string EncryptionPurpose(EncryptionPurpose purpose) => purpose switch
	{
		Models.EncryptionPurpose.Data => T("Данные", "Data"),
		Models.EncryptionPurpose.BlindIndex => T("Слепой индекс", "Blind index"),
		_ => purpose.ToString(),
	};
}
