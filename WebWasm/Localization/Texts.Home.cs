namespace WebWasm.Localization;

public sealed partial class Texts
{
	public HomeTexts Home => field ??= new(Language);
}

/// <summary>Home page: counters, turnover for a period, quick actions, recent activity.</summary>
public sealed class HomeTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Сводка по платформе Kliffort", "Kliffort platform overview");
	public string OrdersToday => T("Заказы сегодня", "Orders today");

	public string Turnover => T("Оборот", "Turnover");
	public string TurnoverHint => T("Сумма заказов за период", "Total cost of orders for the period");
	public string Profit => T("Прибыль до налогов", "Profit before tax");
	public string ProfitHint => T("Комиссия Kliffort за период", "Kliffort commission for the period");
	public string Period => T("Период", "Period");
	public string PeriodStart => T("Начало периода", "Period start");
	public string PeriodEnd => T("Конец периода", "Period end");
	public string AllTime => T("Всё время", "All time");

	/// <summary>Period presets: 1w, 2w, 1m, 2m, 1q, all.</summary>
	public string Preset(string key) => key switch
	{
		"1w" => T("Неделя", "1 week"),
		"2w" => T("2 недели", "2 weeks"),
		"1m" => T("Месяц", "1 month"),
		"2m" => T("2 месяца", "2 months"),
		"1q" => T("Квартал", "Quarter"),
		_ => AllTime,
	};


	public string RecentActivity => T("Последние события", "Recent activity");
	public string NoActivity => T("Событий пока нет", "No activity yet");

	public string ActivityFailed(string reason) => T($"Не удалось загрузить последние события: {reason}", $"Could not load recent activity: {reason}");
	public string OrdersTodayFailed(string reason) => T($"Не удалось загрузить число заказов за сегодня: {reason}", $"Could not load today's order count: {reason}");
	public string UsersFailed(string reason) => T($"Не удалось загрузить число пользователей: {reason}", $"Could not load the user count: {reason}");
	public string CompaniesFailed(string reason) => T($"Не удалось загрузить число компаний: {reason}", $"Could not load the company count: {reason}");
	public string TurnoverFailed(string reason) => T($"Не удалось загрузить оборот: {reason}", $"Could not load the turnover: {reason}");
}
