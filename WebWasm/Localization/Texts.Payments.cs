namespace WebWasm.Localization;

public sealed partial class Texts
{
	public PaymentsTexts Payments => field ??= new(Language);
}

/// <summary>Payments page: bank cards users linked for payment.</summary>
public sealed class PaymentsTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Банковские карты, которые пользователи привязали для оплаты", "Bank cards users linked for payment");
	public string Cards => T("Карты", "Cards");
	public string SearchPlaceholder => T("Поиск по номеру карты", "Search by card number");
	public string CardNumber => T("Номер карты", "Card number");
	public string Expiration => T("Действует до", "Expires");
	public string UnbindAt => T("Дата отвязки", "Unbind date");
	public string Empty => T("Привязанных карт пока нет", "No linked cards yet");
}
