namespace WebWasm.Localization;

public sealed partial class Texts
{
	public PricingTexts Pricing => field ??= new(Language);
	public CapacitiesTexts Capacities => field ??= new(Language);
	public SimulatorTexts Simulator => field ??= new(Language);
	public PriceChartTexts PriceChart => field ??= new(Language);
}

/// <summary>PricingSettings page and the words and number formats shared by the calculator and its chart.</summary>
public sealed class PricingTexts(Language language) : TextBase(language)
{
	private readonly Texts _format = Texts.For(language);

	/// <summary>Number without trailing zeros, at most <paramref name="maxDecimals"/> decimals: 0,2 · 1 500 · 0,0125.</summary>
	public string Number(decimal value, int maxDecimals = 4)
	{
		var rounded = Math.Round(value, maxDecimals);
		var decimals = 0;
		while (decimals < maxDecimals && Math.Round(rounded, decimals) != rounded)
		{
			decimals++;
		}

		return _format.FormatNumber(rounded, decimals);
	}

	public string Number(double value, int maxDecimals = 1) => Number((decimal)value, maxDecimals);

	/// <summary>Setting value or «не задано».</summary>
	public string Value(decimal? value) => value is { } v ? Number(v) : NotSet;

	/// <summary>Short sum for dense places (chart, calculator tables): no kopecks for whole sums and from 1 000 up.</summary>
	public string Money(decimal value) => _format.FormatNumber(value, Math.Abs(value) >= 1000 || value == decimal.Truncate(value) ? 0 : 2);

	public string Tons(double tons) => $"{Number(tons, 2)}\u00A0{T("т", "t")}";

	public string KgAsTons(double kilograms) => Tons(kilograms / 1000);

	/// <summary>Vehicle capacity: whole tons as tons, the rest in kilograms.</summary>
	public string Weight(int kilograms) => kilograms % 1000 == 0
		? KgAsTons(kilograms)
		: $"{Number((decimal)kilograms, 0)}\u00A0{T("кг", "kg")}";

	public string Km(double km) => $"{Number(km, 1)}\u00A0{T("км", "km")}";

	public string Minutes(double minutes) => $"{Number(minutes, 1)}\u00A0{T("мин", "min")}";

	public string Speed(double kmh) => $"{Number(kmh, 1)}\u00A0{T("км/ч", "km/h")}";

	/// <summary>Value already in percent: 5 → «5 %».</summary>
	public string Percent(double percent) => $"{Number(percent, 1)}\u00A0%";

	/// <summary>Share 0…1 in whole percent: 0.35 → «35 %».</summary>
	public string Share(double share) => _format.FormatPercent((decimal)Math.Round(share * 100), 0);

	/// <summary>Signed change: −0.21 → «−21 %», 0.075 → «+7,5 %».</summary>
	public string SignedShare(decimal share)
	{
		var percent = Math.Round(share * 100, 1);
		var sign = percent > 0 ? "+" : percent < 0 ? "−" : string.Empty;
		return $"{sign}{Number(Math.Abs(percent), 1)}\u00A0%";
	}

	public string Range(decimal? min, decimal? max) => (min, max) switch
	{
		(null, null) => Texts.Dash,
		({ } from, null) => $"≥ {Number(from)}",
		(null, { } to) => $"≤ {Number(to)}",
		({ } from, { } to) => $"{Number(from)} … {Number(to)}",
	};

	public string NotSet => T("не задано", "not set");

	public string Subtitle => T(
		"Ставки, сборы и пороги расчёта цены. Значение берётся с самого точного уровня: компания → тип материала → глобальный → по умолчанию. Уровень без своего значения наследует его.",
		"Rates, fees and thresholds of the price calculation. The most specific level wins: company → material type → global → default. A level without its own value inherits it.");
	public string TabsLabel => T("Раздел", "Section");
	public string TabSettings => T("Настройки", "Settings");
	public string TabSimulator => T("Калькулятор", "Calculator");

	public string ScopeLabel => T("Уровень настройки", "Setting level");

	/// <summary>Scope of a setting as the backend sends it: "Global", "Company", "MaterialType".</summary>
	public string ScopeName(string scope) => scope switch
	{
		"Global" => T("Глобальный", "Global"),
		"Company" => T("Компания", "Company"),
		"MaterialType" => T("Тип материала", "Material type"),
		_ => scope,
	};

	public string ChooseCompany => T("Выберите компанию", "Choose a company");
	public string ChooseMaterialType => T("Выберите тип материала", "Choose a material type");
	public string ChooseCompanyHint => T("Здесь появятся значения, заданные для компании, и то, что она наследует.",
		"The values set for the company and what it inherits will appear here.");
	public string ChooseMaterialTypeHint => T("Здесь появятся значения, заданные для типа материала, и то, что он наследует.",
		"The values set for the material type and what it inherits will appear here.");
	public string Empty => T("Настроек цен нет", "No pricing settings");
	public string EmptyHint => T("Сервер не вернул ни одного ключа для этого уровня.", "The server returned no keys for this level.");

	public string Key => T("Ключ", "Key");
	public string Default => T("По умолчанию", "Default");
	public string RangeColumn => T("Диапазон", "Range");
	public string CompanyEffectColumn => T("Значение компании действует на", "Company value applies to");
	public string OverrideColumn(string scope) => T($"Своё значение: {scope}", $"Own value: {scope}");
	public string Effective => T("Действует", "Effective");
	public string Inherited => T("наследуется", "inherited");

	/// <summary>Company role whose override of a key counts: "Buyer", "Supplier", "Carrier".</summary>
	public string RoleEffect(string role) => role switch
	{
		"Buyer" => T("покупатель → комиссия", "buyer → commission"),
		"Supplier" => T("поставщик → материал", "supplier → material"),
		"Carrier" => T("перевозчик → доставка", "carrier → delivery"),
		_ => role,
	};

	public string NoCompanyEffect => T("на компании не действует", "no effect on companies");

	public string SetValue => T("Задать значение", "Set value");
	public string ResetValue => T("Вернуть наследование", "Reset to inherited");
	public string EditTitle(string key, string scope) => $"{key}: {scope}";
	public string ValueLabel => T("Значение", "Value");
	public string ValueHint(string defaultValue, string range) =>
		T($"По умолчанию {defaultValue}, допустимо {range}.", $"Default {defaultValue}, allowed {range}.");
	public string CompanyNoEffectWarning => T(
		"У этого ключа значение компании ни на что не влияет: расчёт возьмёт значение типа материала или глобальное.",
		"For this key a company value has no effect: the calculation takes the material type value or the global one.");
	public string ResetHint => T(
		"Чтобы уровень снова наследовал значение, выберите «Вернуть наследование» в меню строки.",
		"To make the level inherit the value again, choose «Reset to inherited» in the row menu.");
	public string RangeError(string range) => T($"Допустимый диапазон: {range}.", $"Allowed range: {range}.");

	public string LoadFailed(string reason) => T($"Не удалось загрузить настройки цен: {reason}", $"Could not load the pricing settings: {reason}");
	public string Updated(string key) => T($"Значение {key} сохранено", $"{key} saved");
	public string SaveFailed(string reason) => T($"Не удалось сохранить значение: {reason}", $"Could not save the value: {reason}");
	public string ResetDone(string key) => T($"{key} снова наследует значение", $"{key} inherits the value again");
	public string ResetFailed(string key, string reason) =>
		T($"Не удалось вернуть наследование {key}: {reason}", $"Could not reset {key} to inherited: {reason}");

	/// <summary>Buyer kind of a chart line or a calculator row.</summary>
	public string Buyer(bool hasCompany) => hasCompany ? T("с компанией", "with company") : T("без компании", "without company");

	public string NoPrice => T(
		"Нет цены: этого веса нет ни в таблицах региона, ни в ценах тоннажа по умолчанию.",
		"No price: this weight is neither in the region tables nor in the capacity default prices.");
}

/// <summary>VehicleCapacities page: vehicle capacities and their default delivery prices.</summary>
public sealed class CapacitiesTexts(Language language) : TextBase(language)
{
	public string Subtitle => T(
		"Тоннажи машин и цены доставки по умолчанию. Они действуют, когда веса нет в таблице региона.",
		"Vehicle capacities and default delivery prices. They apply when the region table has no such weight.");
	public string Add => T("Добавить тоннаж", "Add capacity");
	public string Empty => T("Тоннажей пока нет", "No capacities yet");
	public string EmptyHint => T("Добавьте первый кнопкой «Добавить тоннаж».", "Add the first one with «Add capacity».");

	public string Weight => T("Тоннаж", "Capacity");
	public string WeightKg => T("Вес, кг", "Weight, kg");
	public string MinPrice => T("Минимальная цена", "Min price");
	public string MaxPrice => T("Максимальная цена", "Max price");
	public string PricesHint => T(
		"Цены одной доставки в BYN. 0 и 0 — цены не настроены: если веса нет в таблице региона, заказ не посчитается.",
		"Prices of one delivery in BYN. 0 and 0 mean not configured: if the region table has no such weight, the order is not priced.");

	public string AddTitle => T("Новый тоннаж", "New capacity");
	public string EditTitle(string weight) => T($"Тоннаж {weight}", $"Capacity {weight}");
	public string DeleteTitle(string weight) => T($"Удалить тоннаж {weight}?", $"Delete capacity {weight}?");
	public string VehiclesQuestion => T("Что сделать с машинами этого тоннажа?", "What to do with the vehicles of this capacity?");
	public string Reassign => T("Перевести машины на другой тоннаж", "Move the vehicles to another capacity");
	public string ReassignTarget => T("Новый тоннаж", "New capacity");
	public string NoTarget => T("Другого тоннажа нет. Сначала добавьте тоннаж, на который перевести машины.",
		"There is no other capacity. Add the capacity to move the vehicles to first.");
	public string DeleteVehicles => T("Удалить машины этого тоннажа", "Delete the vehicles of this capacity");
	public string DeleteVehiclesWarning => T(
		"Машины этого тоннажа будут отключены, а слоты их водителей удалены без возможности восстановления. Водители не будут получать заказы, пока для них не создадут новую машину и слоты.",
		"The vehicles of this capacity will be deactivated and their drivers' slots deleted for good. The drivers get no orders until a new vehicle and slots are created for them.");

	public string WeightPositive => T("Вес — целое число килограммов больше 0.", "The weight must be a whole number of kilograms above 0.");
	public string MaxBelowMin => T("Максимальная цена не может быть меньше минимальной.", "The max price cannot be lower than the min price.");
	public string ChooseTarget => T("Выберите тоннаж, на который перевести машины.", "Choose the capacity to move the vehicles to.");

	public string LoadFailed(string reason) => T($"Не удалось загрузить тоннажи: {reason}", $"Could not load the capacities: {reason}");
	public string Created(string weight) => T($"Тоннаж {weight} добавлен", $"Capacity {weight} added");
	public string Updated(string weight) => T($"Тоннаж {weight} сохранён", $"Capacity {weight} saved");
	public string Deleted(string weight) => T($"Тоннаж {weight} удалён", $"Capacity {weight} deleted");
	public string CreateFailed(string reason) => T($"Не удалось добавить тоннаж: {reason}", $"Could not add the capacity: {reason}");
	public string SaveFailed(string reason) => T($"Не удалось сохранить тоннаж: {reason}", $"Could not save the capacity: {reason}");
	public string DeleteFailed(string reason) => T($"Не удалось удалить тоннаж: {reason}", $"Could not delete the capacity: {reason}");
}

/// <summary>PricingSimulator: the calculator tab of the PricingSettings page.</summary>
public sealed class SimulatorTexts(Language language) : TextBase(language)
{
	public string Participants => T("Участники заказа", "Order participants");
	public string LoadingPlace => T("Площадка погрузки", "Loading place");
	public string Carrier => T("Перевозчик", "Carrier");
	public string Buyer => T("Покупатель", "Buyer");
	public string Required => T("обязательно", "required");
	public string Optional => T("необязательно", "optional");
	public string ChooseLoadingPlace => T("Выберите площадку погрузки", "Choose a loading place");
	public string NoCarrier => T("Не выбран: как при создании заказа", "None: as when the order is created");
	public string NoBuyer => T("Не выбран: компания без своих настроек", "None: a company without its own settings");
	public string ChooseRegion => T("Выберите регион", "Choose a region");
	public string PlaceOption(string producer, string name, string? material, string costPerTon) => T(
		$"{producer} · {name} ({material ?? "материал не задан"}, {costPerTon} за т)",
		$"{producer} · {name} ({material ?? "no material"}, {costPerTon} per t)");
	public string NeedLoadingPlace => T(
		"Выберите площадку погрузки: без неё цена заказа не считается, как и в приложении.",
		"Choose a loading place: without it the order price is not calculated, same as in the app.");
	public string NeedRegion => T(
		"Выберите регион: из него берутся таблицы цен и размеры города и пригорода.",
		"Choose a region: it provides the price tables and the city and suburb sizes.");

	public string ResolvedTitle => T("Какие значения взял расчёт", "Values the calculation used");
	public string ClearOverrides(int count) => T($"Убрать все правки ({count})", $"Remove all changes ({count})");
	public string ResolvedHint => T(
		"Цвет ячейки показывает, откуда взято значение. Одинаковые соседние ячейки склеены. Нажмите на ячейку, чтобы подставить другое значение в памяти: график пересчитается, в базе ничего не изменится. Сохранить подобранное значение можно на вкладке «Настройки».",
		"The cell colour shows where the value comes from. Equal neighbouring cells are merged. Click a cell to try another value in memory: the chart recalculates, nothing changes in the database. Save the value you picked on the «Settings» tab.");
	public string SourceLegend => T("Откуда значение", "Value source");
	public string SourceDefault => T("по умолчанию", "default");
	public string SourceGlobal => T("глобальный", "global");
	public string SourceMaterial => T("материал", "material");
	public string SourceParentMaterial => T("родительский материал", "parent material");
	public string SourceSupplier => T("поставщик", "supplier");
	public string SourceCarrier => T("перевозчик", "carrier");
	public string SourceBuyer => T("покупатель", "buyer");
	public string SourceOverride => T("правка в памяти", "in-memory change");

	public string GroupMaterial => T("Материал", "Material");
	public string GroupMaterialHint => T("Цена поставщика. Его компания влияет только здесь.", "Supplier price. The supplier's company matters only here.");
	public string GroupDelivery => T("Доставка", "Delivery");
	public string GroupDeliveryHint => T(
		"При создании заказа перевозчик неизвестен, поэтому его настройки действуют только в правой колонке.",
		"The carrier is unknown when an order is created, so its settings apply only in the right column.");
	public string StageOnCreate => T("при создании заказа", "on order creation");
	public string StageForCarrier => T("у перевозчика", "for the carrier");
	public string GroupCommission => T("Комиссия платформы", "Platform commission");
	public string GroupCommissionHint => T("Процент зависит от того, есть ли у покупателя компания.", "The percentage depends on whether the buyer has a company.");
	public string StageNoCompany => T("покупатель без компании", "buyer without company");
	public string StageCompany => T("покупатель-компания", "company buyer");
	public string GroupSlots => T("Слоты водителя", "Driver slots");
	public string GroupSlotsHint => T(
		"На цену не влияют: определяют, сколько времени заказ займёт в слоте водителя.",
		"Do not affect the price: they set how long the order takes in the driver's slot.");
	public string StagesJoin => T(" и ", " and ");

	public string NoteChooseCarrier => T(
		"выберите перевозчика в блоке «Участники заказа», чтобы подставить его НДС и сбор",
		"choose a carrier under «Order participants» to apply its VAT and fee");
	public string NoteCompanyIgnored => T("настройки компании не действуют", "company settings do not apply");
	public string NoteNoBuyer => T("компания не выбрана: считается как компания без своих настроек", "no company chosen: counted as a company without its own settings");
	public string NoteBuyerChosen => T("действуют настройки выбранного покупателя", "the chosen buyer's settings apply");

	public string KeyNotRead => T("Формула этого этапа ключ не читает", "This stage's formula does not read the key");
	public string Was(string value, string source) => T($"было {value} · {source}", $"was {value} · {source}");
	public string OverridesTitle => T("Правки в памяти:", "In-memory changes:");
	public string OverrideChip(string value, string scope) => T($"= {value} на уровне «{scope}»", $"= {value} at «{scope}»");
	public string RemoveOverride => T("Убрать правку", "Remove the change");

	public string RegionTitle => T("Регион: цены за одну доставку", "Region: prices per delivery");
	public string RegionHint => T(
		"Почасовая часть цены берёт таблицу города, километровая — таблицу пригорода. Серым — цена тоннажа по умолчанию: веса нет в таблице региона. Нажмите на цену, чтобы подставить другие минимум и максимум в памяти; сохранить их можно в настройках региона.",
		"The hourly part of the price uses the city table, the per-km part uses the suburb table. Grey is the capacity default price: the region table has no such weight. Click a price to try another min and max in memory; save them in the region settings.");
	public string ResetRegionPrices(int count) => T($"Вернуть цены региона ({count})", $"Restore region prices ({count})");
	public string CityColumn => T("Город (по времени), BYN", "City (by time), BYN");
	public string SuburbColumn => T("Пригород (по км), BYN", "Suburb (by km), BYN");
	public string NoPriceCell => T("нет цены", "no price");
	public string PriceDefault => T("цена тоннажа по умолчанию", "capacity default price");
	public string PriceRegion => T("таблица региона", "region table");
	public string RangesHint(string minMinutes, string maxMinutes, string citySpeed, string minKm, string maxKm) => T(
		$"Город: до {minMinutes} в пути — минимальная цена, от {maxMinutes} — максимальная (диагональ города, делённая на скорость в городе {citySpeed}). Пригород: до {minKm} — минимальная, от {maxKm} — максимальная (диагональ пригорода). Между порогами цена растёт линейно.",
		$"City: up to {minMinutes} of travel — min price, from {maxMinutes} — max price (the city diagonal divided by the city speed {citySpeed}). Suburb: up to {minKm} — min, from {maxKm} — max (the suburb diagonal). Between the thresholds the price grows linearly.");

	public string ChartTitle => T("График цены", "Price chart");
	public string Computing => T("Считаю…", "Calculating…");
	public string MaterialParam(string costPerTon) => T($"Материал: {costPerTon} за т", $"Material: {costPerTon} per t");
	public string SupplierRebateParam(string percent) => T($"Скидка поставщика: {percent}", $"Supplier rebate: {percent}");
	public string CarrierRebateParam(string percent) => T($"Скидка перевозчика: {percent}", $"Carrier rebate: {percent}");

	public string OrderWeight => T("Вес заказа", "Order weight");
	public string WeightHint(string progression) => T(
		$"Вес задаёт только число доставок: каждая линия везёт его целыми машинами своего тоннажа с округлением вверх (17 т на машинах по 10 т — 2 доставки по 10 т). Для каждой линии ниже: y — коэффициент применения VolumePriceProgression к этому заказу, на сколько процентов действует значение и на сколько текущее значение ({progression}) меняет доставку и комиссию покупателя-компании.",
		$"The weight only sets the number of deliveries: each line carries it in whole vehicles of its capacity, rounded up (17 t on 10 t vehicles — 2 deliveries of 10 t). For each line below: y, the VolumePriceProgression application factor for this order, how many percent of the value apply, and how much the current value ({progression}) changes delivery and commission of a company buyer.");

	/// <summary>What VolumePriceProgression does to one line: y, the applied share and the price change (already formatted).</summary>
	public string VolumeEffect(int deliveries, string y, string applied, string? change) => deliveries <= 1
		? T("одна доставка, ключ не действует", "one delivery, the key does not apply")
		: change is null
			? T($"y {y}, применяется на {applied}, цены не меняются", $"y {y}, applies at {applied}, prices do not change")
			: T($"y {y}, применяется на {applied}, доставка и комиссия {change}", $"y {y}, applies at {applied}, delivery and commission {change}");

	public string PathModeLabel => T("Что задаёт ползунок пути", "What the route slider sets");
	public string PathDuration => T("Время в пути", "Travel time");
	public string PathDistance => T("Длина маршрута", "Route length");
	public string CityMinMark => T("до этой отметки — минимальная цена города", "below this mark — the city min price");
	public string CityMaxMark => T("от этой отметки — максимальная цена города", "from this mark — the city max price");
	public string SuburbMinMark => T("до этой отметки — минимальная цена пригорода", "below this mark — the suburb min price");
	public string SuburbMaxMark => T("от этой отметки — максимальная цена пригорода", "from this mark — the suburb max price");
	public string CityMarks(string from, string to) => T($"Отметки: {from} и {to} — пороги цены города.", $"Marks: {from} and {to} — the city price thresholds.");
	public string SuburbMarks(string from, string to) => T($"Отметки: {from} и {to} — пороги цены пригорода.", $"Marks: {from} and {to} — the suburb price thresholds.");
	public string SecondValueHint(string circuity, string speed) => T(
		$"Вторая величина одна на весь график: время = км / ({circuity} × {speed}). {circuity} — во сколько раз путь по дорогам в среднем длиннее прямой (по исследованиям дорожных сетей), скорость — AvgTruckSpeedKmh. В настоящем заказе оба числа даёт навигатор.",
		$"The other value is one for the whole chart: time = km / ({circuity} × {speed}). {circuity} is how many times a road route is longer than a straight line on average (road network studies), the speed is AvgTruckSpeedKmh. In a real order the navigator gives both numbers.");
	public string SlotTitle => T("Как SlotHelper вырежет окно из слота водителя. На цену не влияет.", "How SlotHelper cuts the window from the driver's slot. Does not affect the price.");
	public string SlotFormula(string travel, string minutes, string preparation, string roundTrip, string buffer, string trip, string loading) => T(
		$"Окно в слоте водителя при {travel} в пути: подготовка {preparation} + рейс {minutes} × {roundTrip} × (1 + {buffer}) = {trip} + загрузка {loading} →",
		$"Window in the driver's slot at {travel} of travel: preparation {preparation} + trip {minutes} × {roundTrip} × (1 + {buffer}) = {trip} + loading {loading} →");
	public string SlotTail(string minSlot) => T(
		$"(края округляются к ближайшим 5 мин). Остаток слота короче {minSlot} выбрасывается.",
		$"(the edges are rounded to the nearest 5 min). A slot remainder shorter than {minSlot} is dropped.");

	public string Progression => "VolumePriceProgression";
	public string ProgressionSaved => T("сохранённое", "saved");
	public string ProgressionOff => T("Сейчас 0: выключено, цены не меняются ни у одной линии.", "Now 0: off, no line changes its prices.");
	public string ProgressionHint => T(
		"Насколько дешевле (−) или дороже (+) становятся доставка и комиссия в заказе из двух и более доставок: от −1 до 1, то есть не больше чем на 100 %. Сила зависит от y, коэффициента применения: y = 0 — значение действует полностью, y = 1 — никак. Изменение = значение × (1 − y): при y = 0,3 и значении −0,3 доставка и комиссия дешевле на 21 %. y = 1 − e^(−z2 × w × x), z2 = 0,018 / w^0,75, w — средняя грузоподъёмность машин, x — тоннаж со второй доставки. Чем тяжелее заказ, тем y ближе к 1: от ~250 т значение уже ничего не меняет. Заказ из одной доставки не меняется. Меняются цена доставки без НДС и сбора и база комиссии; НДС, сбор и эквайринг начисляются сверху. Пересчёт — когда ползунок отпустили.",
		"How much cheaper (−) or dearer (+) delivery and commission get in an order of two or more deliveries: from −1 to 1, that is by at most 100 %. The strength depends on y, the application factor: y = 0 — the value applies fully, y = 1 — not at all. Change = value × (1 − y): with y = 0.3 and value −0.3 delivery and commission are 21 % cheaper. y = 1 − e^(−z2 × w × x), z2 = 0.018 / w^0.75, w is the average vehicle capacity, x is the tonnage from the second delivery on. The heavier the order, the closer y is to 1: from ~250 t the value changes nothing. An order of one delivery does not change. The delivery price without VAT and fee and the commission base change; VAT, fee and acquiring are added on top. Recalculates when you release the slider.");
	public string RestoreSaved => T("Вернуть сохранённое", "Restore saved");

	public string CustomTitle => T("Заказ из машин разного тоннажа", "Order with mixed capacities");
	public string CustomHint => T(
		"Добавьте доставки нужного тоннажа: заказ появится на графике отдельной линией со звёздами. Разбивка ниже — для точки, выбранной нажатием на график.",
		"Add deliveries of the capacities you need: the order appears on the chart as a separate line with stars. The breakdown below is for the point chosen by clicking the chart.");
	public string AddDelivery(string weight) => T($"Добавить доставку {weight}", $"Add a {weight} delivery");
	public string RemoveDelivery => T("Убрать доставку", "Remove the delivery");
	public string CityShare => T("Доля города", "City share");
	public string TotalColumn => T("Итог, BYN", "Total, BYN");
	public string MaterialColumn => T("Материал, BYN", "Material, BYN");
	public string DeliveriesColumn => T("Доставки, BYN", "Deliveries, BYN");
	public string CarrierGetsColumn => T("Получит перевозчик, BYN", "Carrier gets, BYN");
	public string CommissionColumn => T("Комиссия, BYN", "Commission, BYN");

	public string EditTarget => T("Где подставить (как будто сохранили на этом уровне)", "Where to apply (as if saved at this level)");
	public string EditValueHint(string current, string saved, string savedSource, string range) => T(
		$"Сейчас {current}, сохранено {saved} · {savedSource}, диапазон {range}.",
		$"Now {current}, saved {saved} · {savedSource}, range {range}.");
	public string EditScopeHint => T(
		"Правка на глобальном уровне или на материале меняет все ячейки, которые берут значение оттуда.",
		"A change at the global level or on a material changes every cell that takes the value from there.");
	public string RemoveKeyOverrides => T("Убрать правки ключа", "Remove the key's changes");
	public string Substitute => T("Подставить", "Use value");

	public string RegionEditTitle(string weight, bool city) => city
		? T($"{weight}, город (по времени)", $"{weight}, city (by time)")
		: T($"{weight}, пригород (по км)", $"{weight}, suburb (by km)");
	public string RegionMin => T("Минимальная цена доставки (до нижнего порога), BYN", "Min delivery price (below the lower threshold), BYN");
	public string RegionMax => T("Максимальная цена доставки (от верхнего порога), BYN", "Max delivery price (from the upper threshold), BYN");
	public string InMemoryHint => T("Значение в памяти: график пересчитается, в базе ничего не изменится.", "In-memory value: the chart recalculates, nothing changes in the database.");
	public string RegionPriceError => T("Цены не меньше 0, максимальная не меньше минимальной.", "Prices are at least 0, and the max is not below the min.");

	public string MaterialCostTitle => T("Цена материала за тонну на площадке погрузки", "Material price per ton at the loading place");
	public string SupplierRebateTitle => T("Скидка поставщика, %", "Supplier rebate, %");
	public string CarrierRebateTitle => T("Скидка перевозчика, %", "Carrier rebate, %");
	public string ParamValueLabel => T("Значение в памяти (в базе не меняется)", "In-memory value (the database does not change)");
	public string SupplierRebateHint(string capture) => T(
		$"Плюс — скидка поставщика: материал дешевеет на неё целиком. Доля RebateCapturePercent (сейчас {capture}) этой скидки уходит в комиссию платформы, остальное удешевляет заказ. Минус — наценка на материал, RebateCapturePercent не участвует. Пример: материал 100, скидка 50, захват 0,5 — материал 50, комиссия +25, заказ дешевле на 25.",
		$"Plus is a supplier rebate: the material gets cheaper by all of it. The RebateCapturePercent share (now {capture}) of the rebate goes to the platform commission, the rest makes the order cheaper. Minus is a markup on the material, RebateCapturePercent does not apply. Example: material 100, rebate 50, capture 0.5 — material 50, commission +25, order 25 cheaper.");
	public string CarrierRebateHint(string partsView, string carrierView) => T(
		$"Плюс — эта доля цены доставки остаётся платформе, минус — платформа доплачивает перевозчику. Итог покупателя не меняется: смотрите вид «{partsView}» с галочкой «{carrierView}», он включится сам.",
		$"Plus: this share of the delivery price stays with the platform; minus: the platform pays the carrier extra. The buyer's total does not change: see the «{partsView}» view with «{carrierView}» checked, it turns on by itself.");
	public string NegativePrice => T("Цена не может быть отрицательной.", "The price cannot be negative.");
	public string RebateRange => T("Скидка — от −100 до 100 %.", "The rebate is from −100 to 100 %.");

	public string OptionsFailed(string reason) => T($"Не удалось загрузить списки для калькулятора: {reason}", $"Could not load the calculator lists: {reason}");
	public string ComputeFailed(string reason) => T($"Калькулятор не посчитал цену: {reason}", $"The calculator could not compute the price: {reason}");
	public string ProgressionCellInfo => T(
		"Это значение задаёт ползунок VolumePriceProgression над графиком. Верните ползунок к сохранённому значению, чтобы править его в таблице.",
		"This value is set by the VolumePriceProgression slider above the chart. Restore the slider to the saved value to edit it in the table.");
}

/// <summary>PriceChart: the calculator chart, its toolbar, tooltip, filters and legend.</summary>
public sealed class PriceChartTexts(Language language) : TextBase(language)
{
	public string ModeLabel => T("Вид графика", "Chart view");
	public string ModeTotal => T("Итоговая цена", "Total price");
	public string ModeParts => T("Материал · доставка · комиссия", "Material · delivery · commission");
	public string PerTon => T("Цена за тонну", "Price per ton");
	public string PerTonTitle => T("Итог, делённый на вес заказа линии: так видно, какой тоннаж выгоднее",
		"The total divided by the line's order weight: shows which capacity is cheaper");
	public string Bands => T("Коридор мин…макс", "Min…max band");
	public string BandsTitle => T("Полоса от самой короткой поездки (минимальные цены таблиц) до самой длинной (максимальные)",
		"Band from the shortest trip (table min prices) to the longest one (max prices)");
	public string CarrierView => T("После принятия перевозчиком", "After the carrier accepts");
	public string CarrierViewTitle => T(
		"Все доставки возят машины выбранного перевозчика: доставка — сколько получит он, комиссия — сколько останется платформе. Итог покупателя тот же.",
		"All deliveries go with the chosen carrier's vehicles: delivery is what it gets, commission is what stays with the platform. The buyer's total is the same.");
	public string PartsHint => T(
		"Бледная тонкая линия — итог; точки — материал (М); штрихи — доставки (Д); сплошная — комиссия (К).",
		"The pale thin line is the total; dots are material (M); dashes are deliveries (D); solid is commission (C).");
	public string CarrierPartsHint => T(
		"Сейчас Д — сколько получит перевозчик (его настройки и скидка), К — что останется платформе.",
		"Now D is what the carrier gets (its settings and rebate), C is what stays with the platform.");
	public string MaterialShort => T("М", "M");
	public string DeliveryShort => T("Д", "D");
	public string CommissionShort => T("К", "C");

	public string AriaLabel => T("Цена заказа по доле пути в городе", "Order price by the city share of the route");
	public string XAxis => T("доля пути по городу (цена по времени) →", "city share of the route (priced by time) →");
	public string XAxisShort => T("доля пути по городу →", "city share →");
	public string XAxisLeft => T("← весь путь за городом (цена по км)", "← whole route outside the city (priced by km)");
	public string YAxisOrder => T("цена заказа, BYN", "order price, BYN");
	public string YAxisPerTon => T("цена за тонну, BYN", "price per ton, BYN");

	public string TipTitle(string share) => T($"{share} пути по городу", $"{share} of the route in the city");
	public string PerTonSuffix => T(" за т", " per t");
	public string TipTotal(string total, string low, string high) => T($"{total} (от {low} до {high})", $"{total} (from {low} to {high})");
	public string TipParts(string total, string material, string delivery, string commission) =>
		$"{total}: {MaterialShort} {material} · {DeliveryShort} {delivery} · {CommissionShort} {commission}";
	public string MoreRows(int count) => T($"ещё {count}: отфильтруйте линии ниже", $"{count} more: filter the lines below");
	public string Pinned => T("Закреплено касанием; коснитесь ещё раз, чтобы снять", "Pinned by a tap; tap again to unpin");

	public string FilterBuyer => T("Покупатель", "Buyer");
	public string ShowAll => T("Показать все", "Show all");
	public string CustomOrder => T("свой заказ", "custom order");
	public string CustomSeries(int deliveries, string tons) => T($"свой заказ: {deliveries} дост., {tons}", $"custom order: {deliveries} deliveries, {tons}");
	public string LegendSummary(int visible, int total) => T(
		$"Все линии ({visible} из {total}). Нажмите на линию, чтобы оставить только её; повторное нажатие возвращает все.",
		$"All lines ({visible} of {total}). Click a line to keep only it; click it again to show all.");
}
