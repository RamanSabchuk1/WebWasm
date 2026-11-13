namespace WebWasm.Localization;

public sealed partial class Texts
{
	public ProfileTexts Profile => field ??= new(Language);
}

/// <summary>Profile page (MyUserInfo). Notification history texts live in <see cref="PushTexts"/>.</summary>
public sealed class ProfileTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Ваше имя, уведомления и настройки админки", "Your name, notifications and admin settings");
	public string FirstName => T("Имя", "First name");
	public string MiddleName => T("Отчество", "Middle name");
	public string LastName => T("Фамилия", "Last name");
	public string NoName => T("Имя не заполнено", "Name not set");
	public string EditName => T("Изменить имя", "Edit name");
	public string Settings => T("Настройки", "Settings");
	public string Saved => T("Имя сохранено", "Name saved");
	public string SaveFailed(string reason) => T($"Не удалось сохранить имя: {reason}", $"Could not save the name: {reason}");
}
