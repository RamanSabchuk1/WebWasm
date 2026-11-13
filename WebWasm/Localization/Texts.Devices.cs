namespace WebWasm.Localization;

public sealed partial class Texts
{
	public DevicesTexts Devices => field ??= new(Language);
}

/// <summary>Devices page and DevicesTable: device tokens grouped by user, unbinding one or many.</summary>
public sealed class DevicesTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Устройства, привязанные к пользователям", "Devices bound to users");
	public string SearchPlaceholder => T("Поиск по устройству, пользователю, токену и данным", "Search by device, user, token and data");
	public string Anonymous => T("Без пользователя", "No user");
	public string DeviceCount(int count) => T($"Устройств: {count}", $"{count} device(s)");
	public string SelectGroup(string name) => T($"Выбрать все устройства: {name}", $"Select all devices of {name}");
	public string SelectDevice => T("Выбрать устройство", "Select the device");
	public string Duplicates => T("Есть дубли", "Duplicates");
	public string Duplicate => T("Дубль", "Duplicate");
	public string DuplicatesHint => T("Одно и то же устройство привязано несколькими токенами", "The same device is bound with several tokens");
	public string Token => T("Токен", "Token");
	public string DataItems(int count) => T($"Полей: {count}", $"{count} field(s)");
	public string NoData => T("Нет данных", "No data");
	public string Empty => T("Устройств пока нет", "No devices yet");

	public string Unbind => T("Отвязать", "Unbind");
	public string UnbindSelected(int count) => T($"Отвязать выбранные ({count})", $"Unbind selected ({count})");
	public string ClearSelection => T("Снять выбор", "Clear selection");
	public string NoneSelected => T("Сначала выберите устройства", "Select the devices first");
	public string UnbindTitle => T("Отвязать устройство?", "Unbind the device?");
	public string UnbindManyTitle => T("Отвязать устройства?", "Unbind the devices?");
	public string UnbindMessage(string device) => T(
		$"Устройство «{device}» будет отвязано от пользователя. Действие нельзя отменить.",
		$"«{device}» will be unbound from the user. This cannot be undone.");
	public string UnbindManyMessage(int count) => T(
		$"Будут отвязаны выбранные устройства: {count}. Действие нельзя отменить.",
		$"{count} selected device(s) will be unbound. This cannot be undone.");

	public string Unbound => T("Устройство отвязано", "Device unbound");
	public string UnboundMany(int count) => T($"Отвязано устройств: {count}", $"{count} device(s) unbound");
	public string UnboundPartly(int done, int total, int failed) => T(
		$"Отвязано {done} из {total} устройств, ошибок: {failed}",
		$"Unbound {done} of {total} device(s), {failed} failed");
	public string UnbindFailed(string reason) => T($"Не удалось отвязать устройство: {reason}", $"Could not unbind the device: {reason}");
}
