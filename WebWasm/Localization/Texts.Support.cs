namespace WebWasm.Localization;

public sealed partial class Texts
{
	public SupportTexts Support => field ??= new(Language);
}

/// <summary>Supports page and SuggestionsTable: suggestions users send from the mobile apps.</summary>
public sealed class SupportTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Предложения пользователей из мобильных приложений", "Suggestions users send from the mobile apps");
	public string SearchPlaceholder => T("Поиск по названию и данным", "Search by name and data");
	public string User => T("Пользователь", "User");
	public string Anonymous => T("Аноним", "Anonymous");
	public string Data => T("Данные", "Data");
	public string NoData => T("Нет данных", "No data");
	public string Items(int count) => T($"Полей: {count}", $"{count} field(s)");
	public string Applied => T("Применено", "Applied");
	public string Pending => T("Ждёт", "Pending");
	public string Apply => T("Применить", "Apply");
	public string Empty => T("Предложений пока нет", "No suggestions yet");
	public string AppliedToast => T("Предложение применено", "Suggestion applied");
	public string ApplyFailed(string reason) => T($"Не удалось применить предложение: {reason}", $"Could not apply the suggestion: {reason}");
}
