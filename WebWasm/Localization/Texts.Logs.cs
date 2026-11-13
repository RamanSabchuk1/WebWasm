namespace WebWasm.Localization;

public sealed partial class Texts
{
	public LogsTexts Logs => field ??= new(Language);
}

/// <summary>Logs page: latest server log entries. Log levels and messages are data and stay as the server sends them.</summary>
public sealed class LogsTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Последние записи журнала сервера", "Latest server log entries");
	public string Time => T("Время", "Time");
	public string Level => T("Уровень", "Level");
	public string Message => T("Сообщение", "Message");
	public string CopyTemplate => T("Скопировать шаблон сообщения", "Copy the message template");
	public string ShowException => T("Показать ошибку", "Show the exception");
	public string HideException => T("Скрыть ошибку", "Hide the exception");
	public string Empty => T("В журнале пока нет записей", "No log entries yet");
	public string Loaded => T("Журнал загружен", "Logs loaded");
	public string LoadFailed(string reason) => T($"Не удалось загрузить журнал: {reason}", $"Could not load the logs: {reason}");
}
