namespace WebWasm.Localization;

public sealed partial class Texts
{
	public CompaniesTexts Companies => field ??= new(Language);
}

/// <summary>Companies page, CompaniesTable, CompanyModal.</summary>
public sealed class CompaniesTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Компании покупателей, перевозчиков и поставщиков", "Buyer, carrier and supplier companies");
	public string Add => T("Добавить компанию", "Add company");
	public string SearchPlaceholder => T("Поиск по названию, УНП, почте и адресу", "Search by name, UNP, email and address");
	public string TypeFilter => T("Тип компании", "Company type");
	public string Empty => T("Компаний пока нет", "No companies yet");
	public string EmptyHint => T("Добавьте первую компанию кнопкой «Добавить компанию».", "Add the first one with «Add company».");

	public string Active => T("Активна", "Active");
	public string Inactive => T("Не активна", "Inactive");
	public string Unp => T("УНП", "UNP");
	public string UnpValue(string unp) => T($"УНП {unp}", $"UNP {unp}");
	public string CorporateEmail => T("Корпоративная почта", "Corporate email");
	public string LegalType => T("Правовая форма", "Legal form");
	public string Rebate => T("Скидка", "Rebate");
	public string Location => T("Местоположение", "Location");
	public string Coordinates(string latitude, string longitude) => T($"{latitude}; {longitude}", $"{latitude}, {longitude}");
	public string Details => T("Сведения о компании", "Company details");
	public string BankAccount => T("Банковский счёт", "Bank account");
	public string BankNumber => T("Номер счёта", "Account number");
	public string Bic => T("БИК", "BIC");
	public string PhotoUrl => T("Ссылка на фото", "Photo URL");
	public string PhotoUrlPlaceholder => T("https://… (необязательно)", "https://… (optional)");

	public string AddTitle => T("Новая компания", "New company");
	public string EditTitle => T("Изменить компанию", "Edit company");
	public string CannotChange => T("Нельзя изменить после создания", "Cannot be changed after creation");
	public string EmailPlaceholder => "company@example.com";
	public string LegalTypePlaceholder => T("Например, ООО, ОАО, ИП", "For example, LLC, JSC");
	public string FillRequired => T("Заполните все поля, кроме скидки и фото, и выберите место на карте.",
		"Fill in all fields except the rebate and the photo, and pick the place on the map.");

	public string Activate => T("Активировать", "Activate");
	public string Deactivate => T("Деактивировать", "Deactivate");
	public string ActivateTitle => T("Активировать компанию?", "Activate the company?");
	public string DeactivateTitle => T("Деактивировать компанию?", "Deactivate the company?");
	public string ActivateMessage(string name) => T($"Компания «{name}» станет активной.", $"«{name}» will become active.");
	public string DeactivateMessage(string name) => T($"Компания «{name}» станет неактивной.", $"«{name}» will become inactive.");
	public string DeleteTitle => T("Удалить компанию?", "Delete the company?");
	public string DeleteMessage(string name) => T(
		$"Компания «{name}» будет удалена вместе с её поставщиками, машинами, водителями и пользователями. Быстро отменить это нельзя.",
		$"«{name}» will be deleted together with its suppliers, vehicles, drivers and users. This cannot be easily undone.");

	public string Created => T("Компания добавлена", "Company added");
	public string Updated => T("Компания сохранена", "Company saved");
	public string DeletedToast => T("Компания удалена", "Company deleted");
	public string ActivatedToast => T("Компания активирована", "Company activated");
	public string DeactivatedToast => T("Компания деактивирована", "Company deactivated");
	public string SaveFailed(string reason) => T($"Не удалось сохранить компанию: {reason}", $"Could not save the company: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить компанию: {reason}", $"Could not delete the company: {reason}");
	public string StatusFailed(string reason) => T($"Не удалось изменить статус компании: {reason}", $"Could not change the company status: {reason}");
}
