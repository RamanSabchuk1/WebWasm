namespace WebWasm.Localization;

public sealed partial class Texts
{
	public RegionsTexts Regions => field ??= new(Language);
	public MapsTexts Maps => field ??= new(Language);
}

/// <summary>Regions page, RegionsTable, RegionModal, RegionDetailsModal, LevelEditor, PriceInfoEditor.</summary>
public sealed class RegionsTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Регионы доставки, их уровни, границы на карте и цены", "Delivery regions, their levels, map boundaries and prices");
	public string Add => T("Добавить регион", "Add region");
	public string SearchPlaceholder => T("Поиск по названию", "Search by name");
	public string Levels => T("Уровни", "Levels");
	public string Empty => T("Регионов пока нет", "No regions yet");
	public string EmptyHint => T("Добавьте первый регион кнопкой «Добавить регион».", "Add the first one with «Add region».");

	public string AddTitle => T("Новый регион", "New region");
	public string EditTitle => T("Изменить регион", "Edit region");
	public string NamePlaceholder => T("Например, Минск", "For example, Minsk");
	public string TimeZone => T("Часовой пояс", "Time zone");
	public string TimeZoneHint => T("Идентификатор IANA, например Europe/Minsk", "IANA ID, for example Europe/Minsk");
	public string FillName => T("Введите название региона.", "Enter the region name.");

	public string DetailsTitle(string name) => T($"Регион «{name}»", $"Region «{name}»");
	public string NoLevels => T("У региона пока нет уровней", "The region has no levels yet");
	public string NoLevelsHint => T("Добавьте уровень: нарисуйте его границу на карте и задайте цены.", "Add a level: draw its boundary on the map and set the prices.");
	public string AddLevel => T("Добавить уровень", "Add level");
	public string Triangles => T("Треугольников", "Triangles");
	public string NoBoundary => T("Граница уровня не задана", "The level boundary is not set");
	public string LevelSummary(string algorithm, int triangles) => T($"{algorithm} · треугольников: {triangles}", $"{algorithm} · {triangles} triangle(s)");

	public string Algorithm => T("Алгоритм расчёта", "Pricing algorithm");
	public string AlgorithmName(byte algorithm) => algorithm switch
	{
		1 => T("Цена за км", "Price per km"),
		2 => T("Цена за час", "Price per hour"),
		_ => T("Не задан", "Not set"),
	};

	public string AddLevelTitle => T("Новый уровень", "New level");
	public string EditLevelTitle => T("Изменить уровень", "Edit level");
	public string LevelType => T("Тип уровня", "Level type");
	public string Prices => T("Цены", "Prices");
	public string SavePrices => T("Сохранить цены", "Save prices");
	public string Boundary => T("Граница", "Boundary");
	public string BoundaryPoints(int count) => T($"Точек в границе: {count}", $"Boundary points: {count}");
	public string DrawHint => T("Нажмите «Нарисовать границу» и отметьте точки на карте, нужно минимум 3.", "Press «Draw boundary» and mark at least 3 points on the map.");
	public string DrawBoundary => T("Нарисовать границу", "Draw boundary");
	public string EditBoundary => T("Изменить границу", "Edit boundary");
	public string SaveBoundary => T("Сохранить границу", "Save boundary");
	public string FillLevel => T("Задайте цены и нарисуйте границу из 3 точек или больше.", "Set the prices and draw a boundary of 3 or more points.");
	public string CheckPrices => T("Проверьте цены: в них есть ошибки.", "Check the prices: some of them are invalid.");

	public string PricesCount(int count) => T($"Цен: {count}", $"{count} price(s)");
	public string AddPrice => T("Добавить цену", "Add price");
	public string PriceTitle(int number, uint weight) => T($"{number}. Вес нетто {weight} кг", $"{number}. Net weight {weight} kg");
	public string NetWeight => T("Вес нетто, кг", "Net weight, kg");
	public string NetWeightPlaceholder => T("Например, 1", "For example, 1");
	public string MinPrice => T("Мин. цена, BYN", "Min price, BYN");
	public string MaxPrice => T("Макс. цена, BYN", "Max price, BYN");
	public string DeletePrice => T("Удалить цену", "Delete price");
	public string PriceHasError => T("Есть ошибка", "Has an error");
	public string PriceFilled => T("Заполнено", "Filled");
	public string NoPrices => T("Цен пока нет. Добавьте первую кнопкой «Добавить цену».", "No prices yet. Add the first one with «Add price».");
	public string WeightZero => T("Вес не может быть 0.", "The weight cannot be 0.");
	public string NegativePrice => T("Цена не может быть меньше 0.", "A price cannot be below 0.");
	public string MinAboveMax => T("Мин. цена больше макс. цены.", "The min price is above the max price.");
	public string NeedOnePrice => T("Добавьте хотя бы одну цену.", "Add at least one price.");
	public string DuplicateWeight => T("Вес повторяется: у каждой цены должен быть свой вес.", "A weight repeats: each price needs its own weight.");

	public string DeleteTitle => T("Удалить регион?", "Delete the region?");
	public string DeleteMessage(string name) => T(
		$"Регион «{name}» будет удалён вместе со всеми уровнями. Действие нельзя отменить.",
		$"«{name}» will be deleted with all its levels. This cannot be undone.");
	public string DeleteLevelTitle => T("Удалить уровень?", "Delete the level?");
	public string DeleteLevelMessage(string type, int triangles) => T(
		$"Уровень «{type}» и все его треугольники ({triangles}) будут удалены. Действие нельзя отменить.",
		$"The «{type}» level and all its triangles ({triangles}) will be deleted. This cannot be undone.");

	public string Created => T("Регион добавлен", "Region added");
	public string Updated => T("Регион сохранён", "Region saved");
	public string DeletedToast => T("Регион удалён", "Region deleted");
	public string LevelCreated => T("Уровень добавлен", "Level added");
	public string PricesUpdated => T("Цены сохранены", "Prices saved");
	public string BoundaryUpdated => T("Граница сохранена", "Boundary saved");
	public string LevelDeleted => T("Уровень удалён", "Level deleted");

	public string SaveFailed(string reason) => T($"Не удалось сохранить регион: {reason}", $"Could not save the region: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить регион: {reason}", $"Could not delete the region: {reason}");
	public string LevelSaveFailed(string reason) => T($"Не удалось добавить уровень: {reason}", $"Could not add the level: {reason}");
	public string PricesSaveFailed(string reason) => T($"Не удалось сохранить цены: {reason}", $"Could not save the prices: {reason}");
	public string BoundarySaveFailed(string reason) => T($"Не удалось сохранить границу: {reason}", $"Could not save the boundary: {reason}");
	public string LevelDeleteFailed(string reason) => T($"Не удалось удалить уровень: {reason}", $"Could not delete the level: {reason}");
}

/// <summary>Leaflet maps: LeafletMapBase, LocationMapPicker, PolygonDrawer, TriangleMap.</summary>
public sealed class MapsTexts(Language language) : TextBase(language)
{
	public string LoadFailed(string reason) => T($"Не удалось загрузить карту: {reason}", $"Could not load the map: {reason}");
	public string InitFailed(string reason) => T($"Не удалось показать карту: {reason}", $"Could not show the map: {reason}");
	public string MarkerFailed(string reason) => T($"Не удалось передвинуть метку: {reason}", $"Could not move the marker: {reason}");

	public string Latitude => T("Широта", "Latitude");
	public string Longitude => T("Долгота", "Longitude");
	public string PickHint => T("Нажмите на карту или перетащите метку, чтобы выбрать точку.", "Click the map or drag the marker to choose the point.");

	/// <summary>Latitude and longitude already formatted: in Russian the decimal comma needs another separator.</summary>
	public string Coordinates(string latitude, string longitude) => T($"{latitude}; {longitude}", $"{latitude}, {longitude}");
	public string Points(int count) => T($"Точки: {count}", $"Points: {count}");
	public string NoPoints => T("Нажмите на карту, чтобы добавить точку.", "Click the map to add a point.");
	public string DeletePoint => T("Удалить точку", "Delete point");
	public string Undo => T("Отменить", "Undo");
	public string UndoHint => T("Убрать последнюю точку", "Remove the last point");
	public string ApplyBoundary => T("Применить границу", "Use this boundary");
	public string MinPoints => T("Для границы нужно минимум 3 точки.", "A boundary needs at least 3 points.");
	public string Triangle => T("Треугольник", "Triangle");
}
