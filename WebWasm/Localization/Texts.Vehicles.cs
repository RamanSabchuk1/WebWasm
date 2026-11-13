namespace WebWasm.Localization;

public sealed partial class Texts
{
	public VehiclesTexts Vehicles => field ??= new(Language);
}

/// <summary>Vehicles page, VehiclesTable, VehicleModal.</summary>
public sealed class VehiclesTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Машины перевозчиков и их водители", "Carrier vehicles and their drivers");
	public string Add => T("Добавить машину", "Add vehicle");
	public string SearchPlaceholder => T("Поиск по модели, госномеру и водителю", "Search by model, plate number and driver");
	public string Empty => T("Машин пока нет", "No vehicles yet");
	public string EmptyHint => T("Добавьте первую машину кнопкой «Добавить машину».", "Add the first one with «Add vehicle».");

	public string Model => T("Модель", "Model");
	public string Registration => T("Госномер", "Plate number");
	public string Driver => T("Водитель", "Driver");
	public string NoDriver => T("Водитель не назначен", "No driver assigned");
	public string ShowDriver => T("Показать данные водителя", "Show driver details");
	public string HideDriver => T("Скрыть данные водителя", "Hide driver details");
	public string DriverName => T("Имя", "Name");
	public string Weight => T("Масса, кг", "Weight, kg");
	public string Capacity => T("Тоннаж, кг", "Capacity, kg");
	public string ShowPhoto => T("Показать фото", "Show photo");
	public string HidePhoto => T("Скрыть фото", "Hide photo");
	public string NoPhoto => T("Нет фото", "No photo");

	public string AddTitle => T("Новая машина", "New vehicle");
	public string ModelPlaceholder => T("Например, МАЗ-5516", "For example, MAZ-5516");
	public string SelectCapacity => T("Выберите тоннаж", "Select a capacity");
	public string CapacityOption(string kilograms) => T($"{kilograms} кг", $"{kilograms} kg");
	public string CapacitiesFailed(string reason) => T($"Не удалось загрузить тоннажи: {reason}", $"Could not load the capacities: {reason}");
	public string SelectDriver => T("Выберите водителя", "Select a driver");
	public string UnnamedDriver => T("Без имени", "No name");
	public string PhotoUrl => T("Ссылка на фото", "Photo URL");
	public string PhotoUrlPlaceholder => T("https://… (необязательно)", "https://… (optional)");

	public string DeleteTitle => T("Удалить машину?", "Delete the vehicle?");
	public string DeleteMessage(string model, string registration) => T(
		$"Машина {model} ({registration}) будет удалена. Действие нельзя отменить.",
		$"{model} ({registration}) will be deleted. This cannot be undone.");

	public string Created => T("Машина добавлена", "Vehicle added");
	public string DeletedToast => T("Машина удалена", "Vehicle deleted");
	public string NoCompany => T("Не удалось определить компанию водителя. Выберите другого водителя.", "Could not find the driver's company. Choose another driver.");
	public string CreateFailed(string reason) => T($"Не удалось добавить машину: {reason}", $"Could not add the vehicle: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить машину: {reason}", $"Could not delete the vehicle: {reason}");
}
