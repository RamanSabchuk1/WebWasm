namespace WebWasm.Localization;

public sealed partial class Texts
{
	public ProvidersTexts Providers => field ??= new(Language);
}

/// <summary>Providers page (suppliers), ProducersTable, ProducerModal, LoadingPlaceModal.</summary>
public sealed class ProvidersTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Поставщики материалов и их площадки погрузки", "Material suppliers and their loading places");
	public string Add => T("Добавить поставщика", "Add supplier");
	public string SearchPlaceholder => T("Поиск по поставщику, площадке и материалу", "Search by supplier, loading place and material");
	public string Empty => T("Поставщиков пока нет", "No suppliers yet");
	public string EmptyHint => T("Добавьте первого поставщика кнопкой «Добавить поставщика».", "Add the first one with «Add supplier».");

	public string Supplier => T("Поставщик", "Supplier");
	public string NoCompany => T("Без компании", "No company");
	public string WorkingHoursCount(int count) => T($"Часы работы ({count})", $"Working hours ({count})");
	public string LoadingPlacesCount(int count) => T($"Площадки погрузки ({count})", $"Loading places ({count})");
	public string AddPlace => T("Добавить площадку", "Add loading place");
	public string Day => T("День", "Day");
	public string LoadingHours => T("Часы погрузки", "Loading hours");
	public string WorkingHours => T("Часы работы", "Working hours");
	public string TimeRange(string from, string to) => $"{from}–{to}";
	public string Cost => T("Стоимость", "Cost");
	public string Volume => T("Объём", "Volume");
	public string VolumeValue(string volume) => T($"{volume} м³", $"{volume} m³");
	public string Material => T("Материал", "Material");
	public string Coordinates(string latitude, string longitude) => T($"{latitude}; {longitude}", $"{latitude}, {longitude}");

	public string DayShort(DayOfWeek day) => day switch
	{
		DayOfWeek.Monday => T("Пн", "Mon"),
		DayOfWeek.Tuesday => T("Вт", "Tue"),
		DayOfWeek.Wednesday => T("Ср", "Wed"),
		DayOfWeek.Thursday => T("Чт", "Thu"),
		DayOfWeek.Friday => T("Пт", "Fri"),
		DayOfWeek.Saturday => T("Сб", "Sat"),
		DayOfWeek.Sunday => T("Вс", "Sun"),
		_ => day.ToString(),
	};

	public string DayName(DayOfWeek day) => day switch
	{
		DayOfWeek.Monday => T("Понедельник", "Monday"),
		DayOfWeek.Tuesday => T("Вторник", "Tuesday"),
		DayOfWeek.Wednesday => T("Среда", "Wednesday"),
		DayOfWeek.Thursday => T("Четверг", "Thursday"),
		DayOfWeek.Friday => T("Пятница", "Friday"),
		DayOfWeek.Saturday => T("Суббота", "Saturday"),
		DayOfWeek.Sunday => T("Воскресенье", "Sunday"),
		_ => day.ToString(),
	};

	public string AddTitle => T("Новый поставщик", "New supplier");
	public string EditTitle => T("Изменить поставщика", "Edit supplier");
	public string NameLocked => T("Название нельзя изменить", "The name cannot be changed");
	public string CompanyLocked => T("Компанию нельзя изменить", "The company cannot be changed");
	public string AddWorkingTime => T("Добавить время работы", "Add working time");
	public string NoWorkingTimes => T("Время работы не задано", "No working time yet");
	public string NoWorkingTimesHint => T("Добавьте хотя бы один день кнопкой «Добавить время работы».", "Add at least one day with «Add working time».");
	public string Entry(int number) => T($"Запись {number}", $"Entry {number}");
	public string RemoveEntry => T("Удалить запись", "Remove entry");
	public string Weekday => T("День недели", "Day of week");
	public string LoadingStart => T("Начало погрузки", "Loading starts");
	public string LoadingEnd => T("Конец погрузки", "Loading ends");
	public string WorkingStart => T("Начало работы", "Work starts");
	public string WorkingEnd => T("Конец работы", "Work ends");
	public string FillRequired => T("Укажите название и добавьте хотя бы одно время работы. Начало должно быть раньше конца.",
		"Enter the name and add at least one working time. The start must be before the end.");

	public string PlaceAddTitle => T("Новая площадка погрузки", "New loading place");
	public string PlaceEditTitle => T("Изменить площадку погрузки", "Edit loading place");
	public string SelectMaterial => T("Выберите материал", "Select a material");
	public string CostLabel => T("Стоимость, BYN", "Cost, BYN");
	public string VolumeLabel => T("Объём, м³", "Volume, m³");
	public string Location => T("Местоположение", "Location");
	public string PlaceFillRequired => T("Заполните все поля и выберите место на карте.", "Fill in all fields and pick the place on the map.");

	public string DeleteTitle => T("Удалить поставщика?", "Delete the supplier?");
	public string DeleteMessage(string name) => T($"Поставщик «{name}» будет удалён. Действие нельзя отменить.", $"«{name}» will be deleted. This cannot be undone.");
	public string DeleteWithPlacesMessage(string name, int count) => T(
		$"Поставщик «{name}» и его площадки погрузки ({count}) будут удалены. Действие нельзя отменить.",
		$"«{name}» and its {count} loading place(s) will be deleted. This cannot be undone.");
	public string DeletePlaceTitle => T("Удалить площадку погрузки?", "Delete the loading place?");
	public string DeletePlaceMessage(string name) => T($"Площадка «{name}» будет удалена. Действие нельзя отменить.", $"«{name}» will be deleted. This cannot be undone.");

	public string Created => T("Поставщик добавлен", "Supplier added");
	public string Updated => T("Поставщик сохранён", "Supplier saved");
	public string DeletedToast => T("Поставщик удалён", "Supplier deleted");
	public string SaveFailed(string reason) => T($"Не удалось сохранить поставщика: {reason}", $"Could not save the supplier: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить поставщика: {reason}", $"Could not delete the supplier: {reason}");
	public string PlaceCreated => T("Площадка погрузки добавлена", "Loading place added");
	public string PlaceUpdated => T("Площадка погрузки сохранена", "Loading place saved");
	public string PlaceDeleted => T("Площадка погрузки удалена", "Loading place deleted");
	public string PlaceSaveFailed(string reason) => T($"Не удалось сохранить площадку погрузки: {reason}", $"Could not save the loading place: {reason}");
	public string PlaceDeleteFailed(string reason) => T($"Не удалось удалить площадку погрузки: {reason}", $"Could not delete the loading place: {reason}");
}
