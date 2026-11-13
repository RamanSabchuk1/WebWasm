using System.Globalization;
using Microsoft.AspNetCore.Components;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

/// <summary>
/// Калькулятор цен (вкладка «Калькулятор» страницы Pricing Settings). SA выбирает площадку погрузки, перевозчика и покупателя,
/// видит, во что зарезолвились настройки и откуда (цвет ячейки), подставляет значения в памяти и смотрит график цен
/// по доле пути в городе. Считает backend (та же формула, что у настоящих заказов): POST admin/pricing-settings/simulation;
/// ползунки — с задержкой, устаревшие ответы отбрасываются. Ничего не сохраняет.
/// </summary>
public partial class PricingSimulator : IDisposable
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;

	private const int DebounceMs = 250;
	private const int MaxCustomDeliveries = 30;
	private const string Duration = "Duration";
	private const string Distance = "Distance";
	private const string City = "City";
	private const string Neighborhood = "Neighborhood";

	private PricingSimulationOptions? _options;
	private PricingSimulationResult? _result;
	private string? _error;
	private bool _busy;
	private int _sequence;
	private CancellationTokenSource? _debounce;

	private Guid? _loadingPlaceId;
	private Guid? _carrierId;
	private Guid? _buyerId;
	private Guid? _regionId;

	private double _weightTons = 30;
	private string _pathMode = Duration;
	private double _minutes = 30;
	private double _km = 30;
	private decimal? _progression;
	private double _progressionDraft;
	private bool _carrierView;
	private int _selectedIndex = -1;

	private decimal? _materialCost;
	private double? _supplierRebate;
	private double? _carrierRebate;

	private readonly List<OverrideView> _overrides = [];
	private readonly List<uint> _custom = [];
	private readonly List<PricingSimulationRegionPriceOverride> _regionPrices = [];

	// Правка цены тоннажа в таблице региона.
	private (uint WeightKg, string Level)? _regionEdit;
	private decimal _regionMin;
	private decimal _regionMax;
	private string? _regionError;

	// Правка ячейки таблицы.
	private PricingSimulationSetting? _editSetting;
	private PricingSimulationCell? _editCell;
	private decimal _editValue;
	private int _editTarget;
	private string? _editError;

	// Всплывающее поле параметра, которого нет в pricing settings.
	private ParamKind? _param;
	private decimal _paramValue;
	private string? _paramError;

	private enum ParamKind
	{
		MaterialCost,
		SupplierRebate,
		CarrierRebate,
	}

	private sealed record OverrideView(PricingSettingOverride Override, string ScopeName);

	private sealed record ColumnInfo(string Id, string Stage);

	/// <summary>Группа таблицы — составляющая цены; колонки — этапы, на которых ключ резолвится по-разному.</summary>
	private sealed record GroupInfo(string Title, string Hint, ColumnInfo[] Columns);

	private GroupInfo[] Groups =>
	[
		new(T.Simulator.GroupMaterial, T.Simulator.GroupMaterialHint, [new("Material", "")]),
		new(T.Simulator.GroupDelivery, T.Simulator.GroupDeliveryHint,
		[
			new("DeliveryOnCreate", T.Simulator.StageOnCreate),
			new("DeliveryForCarrier", T.Simulator.StageForCarrier),
		]),
		new(T.Simulator.GroupCommission, T.Simulator.GroupCommissionHint,
		[
			new("CommissionNoCompany", T.Simulator.StageNoCompany),
			new("CommissionCompany", T.Simulator.StageCompany),
		]),
		new(T.Simulator.GroupSlots, T.Simulator.GroupSlotsHint, [new("SlotsOnly", "")]),
	];

	private PricingSimulationLoadingPlace? LoadingPlace => _options?.LoadingPlaces.FirstOrDefault(x => x.Id == _loadingPlaceId);
	private PricingSimulationCompany[] Carriers => _options is null ? [] : [.. _options.Companies.Where(x => x.IsCarrier)];
	private PricingSimulationCompany[] Buyers => _options?.Companies ?? [];
	private int OverrideCount => _overrides.Count;
	private bool IsDuration => _pathMode == Duration;
	private double PathValue => IsDuration ? _minutes : _km;

	protected override async Task OnInitializedAsync()
	{
		try
		{
			_options = await ApiClient.Get<PricingSimulationOptions>("admin/pricing-settings/simulation/options");
			if (_options.Regions.Length == 1)
			{
				_regionId = _options.Regions[0].Id;
			}
		}
		catch (Exception ex)
		{
			_error = ex.Message;
			ToastService.ShowError(T.Simulator.OptionsFailed(ex.Message));
		}
	}

	public void Dispose()
	{
		_debounce?.Cancel();
		_debounce?.Dispose();
	}

	private static Guid? ParseGuid(ChangeEventArgs e) => Guid.TryParse(e.Value?.ToString(), out var id) ? id : null;

	private static double ParseDouble(ChangeEventArgs e, double fallback) =>
		double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;

	private async Task OnLoadingPlaceChanged(ChangeEventArgs e)
	{
		_loadingPlaceId = ParseGuid(e);
		_materialCost = null;
		_supplierRebate = null;
		if (LoadingPlace is { } place && _options!.Regions.Any(r => r.Id == place.RegionId) && _regionId != place.RegionId)
		{
			_regionId = place.RegionId;
			_regionPrices.Clear();
		}

		await Recompute();
	}

	private async Task OnCarrierChanged(ChangeEventArgs e)
	{
		_carrierId = ParseGuid(e);
		_carrierRebate = null;
		_carrierView = _carrierView && _carrierId is not null;
		await Recompute();
	}

	private async Task OnBuyerChanged(ChangeEventArgs e)
	{
		_buyerId = ParseGuid(e);
		await Recompute();
	}

	private async Task OnRegionChanged(ChangeEventArgs e)
	{
		_regionId = ParseGuid(e);
		// Правки цен тоннажей относятся к таблицам прежнего региона.
		_regionPrices.Clear();
		await Recompute();
	}

	private void OnWeightInput(ChangeEventArgs e)
	{
		_weightTons = Math.Max(0.1, ParseDouble(e, _weightTons));
		ScheduleRecompute();
	}

	private void OnPathInput(ChangeEventArgs e)
	{
		if (IsDuration)
		{
			_minutes = ParseDouble(e, _minutes);
		}
		else
		{
			_km = ParseDouble(e, _km);
		}

		ScheduleRecompute();
	}

	private async Task SetPathMode(string mode)
	{
		if (_pathMode == mode)
		{
			return;
		}

		// Переключение сохраняет поездку: вторую величину backend уже посчитал (одна на все точки графика).
		if (_result?.Ranges is { } r && PointAtSelected() is { } point)
		{
			if (mode == Distance)
			{
				_km = Math.Round(point.Km);
			}
			else
			{
				_minutes = Math.Round(point.Minutes);
			}

			_km = Math.Min(_km, r.KmLimit);
			_minutes = Math.Min(_minutes, r.MinutesLimit);
		}

		_pathMode = mode;
		await Recompute();
	}

	// Ползунок прогрессии пересчитывает график, когда его отпустили: при перетаскивании меняется только подпись.
	private void OnProgressionInput(ChangeEventArgs e) => _progressionDraft = ParseDouble(e, _progressionDraft);

	private async Task OnProgressionChange(ChangeEventArgs e)
	{
		_progressionDraft = ParseDouble(e, _progressionDraft);
		_progression = (decimal)Math.Round(_progressionDraft, 2);
		await Recompute();
	}

	private async Task ResetProgression()
	{
		_progression = null;
		await Recompute();
	}

	private void ScheduleRecompute()
	{
		_debounce?.Cancel();
		_debounce?.Dispose();
		_debounce = new CancellationTokenSource();
		_ = Debounced(_debounce.Token);
	}

	private async Task Debounced(CancellationToken token)
	{
		try
		{
			await Task.Delay(DebounceMs, token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		await Recompute();
	}

	private async Task Recompute()
	{
		if (_loadingPlaceId is not { } loadingPlaceId || _regionId is not { } regionId)
		{
			_result = null;
			return;
		}

		var sequence = ++_sequence;
		_busy = true;
		await InvokeAsync(StateHasChanged);
		try
		{
			var input = new PricingSimulationInput(
				loadingPlaceId,
				regionId,
				_buyerId,
				_carrierId,
				_weightTons,
				_pathMode,
				PathValue,
				_materialCost,
				_supplierRebate,
				_carrierRebate,
				_progression,
				[.. _overrides.Select(x => x.Override)],
				[.. _custom],
				[.. _regionPrices]);
			var result = await ApiClient.Post<PricingSimulationInput, PricingSimulationResult>("admin/pricing-settings/simulation", input);
			if (sequence != _sequence)
			{
				return;
			}

			_result = result;
			_error = null;
			_progressionDraft = (double)result.VolumePriceProgression;
			ClampSliders(result.Ranges);
		}
		catch (Exception ex)
		{
			if (sequence == _sequence)
			{
				_error = ex.Message;
				ToastService.ShowError(T.Simulator.ComputeFailed(ex.Message));
			}
		}
		finally
		{
			if (sequence == _sequence)
			{
				_busy = false;
			}

			await InvokeAsync(StateHasChanged);
		}
	}

	private void ClampSliders(PricingSimulationRanges ranges)
	{
		_km = Math.Min(_km, ranges.KmLimit);
		_minutes = Math.Min(_minutes, ranges.MinutesLimit);
		_weightTons = Math.Clamp(_weightTons, ranges.MinTons, ranges.MaxTons);
	}

	/// <summary>Число для атрибутов ползунка: всегда с точкой, браузер другой формат не примет.</summary>
	private static string Inv(double value) => value.ToString(CultureInfo.InvariantCulture);

	/// <summary>Положение отметки на ползунке в процентах ширины.</summary>
	private static string MarkLeft(double value, double min, double max) =>
		(max <= min ? 0 : Math.Clamp((value - min) / (max - min) * 100, 0, 100)).ToString("0.#", CultureInfo.InvariantCulture) + "%";

	// --- подписи ---

	private string Money(decimal value) => T.Pricing.Money(value);

	private string PlaceLabel(PricingSimulationLoadingPlace place) =>
		T.Simulator.PlaceOption(place.ProducerName, place.Name, place.MaterialName, Money(place.CostPerTon));

	private string ProgressionText => T.FormatNumber(_progressionDraft, 2);

	private string ProgressionLabel => _progression is null ? $"{ProgressionText} ({T.Simulator.ProgressionSaved})" : ProgressionText;

	private static string? PriceClass(PricingSimulationPrice price) => price.IsOverride ? "src-override" : price.FromDefault ? "price-default" : null;

	private string PriceTitle(PricingSimulationPrice price) =>
		price.IsOverride ? T.Simulator.SourceOverride : price.FromDefault ? T.Simulator.PriceDefault : T.Simulator.PriceRegion;

	/// <summary>Что VolumePriceProgression делает с линией: y, доля применения и изменение доставки и комиссии (multiplier − 1).</summary>
	private string VolumeEffect(int deliveries, double y, decimal multiplier)
	{
		var change = multiplier - 1m;
		return T.Simulator.VolumeEffect(deliveries, T.FormatNumber(y, 2), T.Pricing.Share(1 - y), change == 0m ? null : T.Pricing.SignedShare(change));
	}

	// --- точка графика, выбранная кликом (по умолчанию середина оси) ---

	private int SelectedIndex(PricingSimulationSeries series) =>
		series.Points.Length == 0 ? -1 : _selectedIndex >= 0 && _selectedIndex < series.Points.Length ? _selectedIndex : series.Points.Length / 2;

	private PricingSimulationPoint? PointAtSelected() =>
		_result?.Series.FirstOrDefault(s => s.Points.Length > 0) is { } s ? s.Points[SelectedIndex(s)] : null;

	private void OnPointSelected(int index) => _selectedIndex = index;

	/// <summary>Окно в слоте водителя для времени в пути выбранной точки — как его вырежет SlotHelper на backend.</summary>
	private (double Trip, int Window, double Minutes)? SlotWindow()
	{
		if (_result is not { } r || PointAtSelected() is not { } point)
		{
			return null;
		}

		var trip = point.Minutes * r.Slot.RoundTripFactor * (1 + r.Slot.TrafficBuffer);
		return (trip, PriceChartMath.SlotWindowMinutes(r.Slot.PreparationMinutes, trip + r.Slot.LoadingMinutes), point.Minutes);
	}

	// --- таблица резолюции ---

	/// <summary>Пояснение к колонке целиком: почему настройки компании тут действуют или нет.</summary>
	private string? ColumnNote(string column) => column switch
	{
		"DeliveryForCarrier" when _carrierId is null => T.Simulator.NoteChooseCarrier,
		"DeliveryForCarrier" => Carriers.FirstOrDefault(x => x.Id == _carrierId)?.Name,
		"CommissionNoCompany" => T.Simulator.NoteCompanyIgnored,
		"CommissionCompany" when _buyerId is null => T.Simulator.NoteNoBuyer,
		"CommissionCompany" => T.Simulator.NoteBuyerChosen,
		_ => null,
	};

	private PricingSimulationSetting[] RowsOf(GroupInfo group) => _result is null
		? []
		: [.. _result.Settings.Where(s => s.Cells.Any(c => group.Columns.Any(col => col.Id == c.Column)))];

	private static PricingSimulationCell?[] CellsOf(PricingSimulationSetting setting, GroupInfo group) =>
		[.. group.Columns.Select(col => setting.Cells.FirstOrDefault(c => c.Column == col.Id))];

	/// <summary>Этапы склеенной ячейки: «при создании заказа и у перевозчика» (подпись ячейки на телефоне).</summary>
	private string StagesOf(GroupInfo group, int start, int length) =>
		string.Join(T.Simulator.StagesJoin, group.Columns.Skip(start).Take(length).Select(c => c.Stage).Where(x => x.Length > 0));

	/// <summary>
	/// Склеенная ячейка правится на любом скоупе любой из её колонок: иначе компания перевозчика пропадала из списка,
	/// когда его значение совпадало со значением при создании заказа. Компании — первыми, как самые специфичные.
	/// </summary>
	private static PricingSimulationCell Merged(PricingSimulationCell?[] cells, int start, int length)
	{
		var run = cells.Skip(start).Take(length).OfType<PricingSimulationCell>().ToArray();
		PricingSimulationEditTarget[] targets =
		[
			.. run.SelectMany(c => c.EditTargets)
				.DistinctBy(t => (t.Scope, t.ScopeId))
				.OrderBy(t => t.Scope == "Company" ? 0 : 1),
		];
		return run[0] with { EditTargets = targets };
	}

	private static bool SameCell(PricingSimulationCell a, PricingSimulationCell b) =>
		a.Value == b.Value && a.Note == b.Note && a.SavedValue == b.SavedValue
		&& a.Source.Scope == b.Source.Scope && a.Source.ScopeId == b.Source.ScopeId && a.Source.IsOverride == b.Source.IsOverride;

	/// <summary>Цепочка материала площадки погрузки, лист первым — для оттенков цвета материала.</summary>
	private Guid[] MaterialChain => _result?.Settings
		.SelectMany(s => s.Cells)
		.Select(c => c.EditTargets.Where(t => t.Scope == "MaterialType").Select(t => t.ScopeId!.Value).ToArray())
		.FirstOrDefault(x => x.Length > 0) ?? [];

	private string SourceClass(PricingSimulationCell cell)
	{
		var source = cell.Source;
		if (source.IsOverride)
		{
			return "src-override";
		}

		return source.Scope switch
		{
			null => "src-default",
			"Global" => "src-global",
			"MaterialType" => $"src-material-{Math.Min(2, Math.Max(0, Array.IndexOf(MaterialChain, source.ScopeId ?? Guid.Empty)))}",
			"Company" when source.ScopeId == _carrierId => "src-carrier",
			"Company" when cell.Role == "Buyer" => "src-buyer",
			"Company" => "src-supplier",
			_ => "src-default",
		};
	}

	private static bool ChangedFromSaved(PricingSimulationCell cell) =>
		cell.Value != cell.SavedValue || cell.Source.Scope != cell.SavedSource.Scope || cell.Source.ScopeId != cell.SavedSource.ScopeId;

	private bool EditKeyHasOverrides => _editSetting is { } setting && _overrides.Any(x => x.Override.Key == setting.Key);

	private void OpenEdit(PricingSimulationSetting setting, PricingSimulationCell cell)
	{
		// Значение ползунка графика правится самим ползунком.
		if (cell.Source.IsOverride && cell.Source.Scope is null)
		{
			ToastService.ShowInfo(T.Simulator.ProgressionCellInfo);
			return;
		}

		if (cell.EditTargets.Length == 0)
		{
			return;
		}

		_editSetting = setting;
		_editCell = cell;
		_editValue = cell.Value ?? 0;
		_editError = null;
		// Если значение уже подставлено на одном из скоупов — открываем на нём, иначе на самом специфичном.
		var current = Array.FindIndex(cell.EditTargets, t => t.Scope == cell.Source.Scope && t.ScopeId == cell.Source.ScopeId);
		_editTarget = cell.Source.IsOverride && current >= 0 ? current : 0;
	}

	private void CloseEdit()
	{
		_editSetting = null;
		_editCell = null;
	}

	private void OnEditTargetChanged(ChangeEventArgs e) => _editTarget = int.TryParse(e.Value?.ToString(), out var index) ? index : 0;

	private async Task ApplyEdit()
	{
		if (_editSetting is not { } setting || _editCell is not { } cell)
		{
			return;
		}

		if ((setting.Min is { } min && _editValue < min) || (setting.Max is { } max && _editValue > max))
		{
			_editError = T.Pricing.RangeError(T.Pricing.Range(setting.Min, setting.Max));
			return;
		}

		var target = cell.EditTargets[Math.Clamp(_editTarget, 0, cell.EditTargets.Length - 1)];
		_overrides.RemoveAll(x => x.Override.Key == setting.Key && x.Override.Scope == target.Scope && x.Override.ScopeId == target.ScopeId);
		_overrides.Add(new OverrideView(new PricingSettingOverride(target.Scope, target.ScopeId, setting.Key, _editValue), target.Name));
		CloseEdit();
		await Recompute();
	}

	private async Task RemoveKeyOverrides()
	{
		if (_editSetting is not { } setting)
		{
			return;
		}

		_overrides.RemoveAll(x => x.Override.Key == setting.Key);
		CloseEdit();
		await Recompute();
	}

	private async Task RemoveOverride(OverrideView view)
	{
		_overrides.Remove(view);
		await Recompute();
	}

	private async Task ClearOverrides()
	{
		_overrides.Clear();
		await Recompute();
	}

	// --- параметры вне pricing settings ---

	private decimal? RebateCapture => _result?.Settings.FirstOrDefault(x => x.Key == "RebateCapturePercent")?.Cells.FirstOrDefault()?.Value;

	private void OpenParam(ParamKind kind)
	{
		_param = kind;
		_paramError = null;
		_paramValue = kind switch
		{
			ParamKind.MaterialCost => _result?.MaterialCostPerTon ?? LoadingPlace?.CostPerTon ?? 0,
			ParamKind.SupplierRebate => (decimal)(_result?.SupplierRebatePercent ?? 0),
			_ => (decimal)(_result?.CarrierRebatePercent ?? 0),
		};
	}

	private void CloseParam() => _param = null;

	private string ParamTitle => _param switch
	{
		ParamKind.MaterialCost => T.Simulator.MaterialCostTitle,
		ParamKind.SupplierRebate => T.Simulator.SupplierRebateTitle,
		_ => T.Simulator.CarrierRebateTitle,
	};

	private bool ParamChanged(ParamKind kind) => kind switch
	{
		ParamKind.MaterialCost => _materialCost is not null,
		ParamKind.SupplierRebate => _supplierRebate is not null,
		_ => _carrierRebate is not null,
	};

	private async Task ApplyParam(bool reset)
	{
		if (!reset)
		{
			// Границы — как у API компаний (Rebate от −100 до 100) и неотрицательная цена.
			if (_param is ParamKind.MaterialCost && _paramValue < 0)
			{
				_paramError = T.Simulator.NegativePrice;
				return;
			}

			if (_param is ParamKind.SupplierRebate or ParamKind.CarrierRebate && _paramValue is < -100 or > 100)
			{
				_paramError = T.Simulator.RebateRange;
				return;
			}
		}

		switch (_param)
		{
			case ParamKind.MaterialCost:
				_materialCost = reset ? null : _paramValue;
				break;
			case ParamKind.SupplierRebate:
				_supplierRebate = reset ? null : (double)_paramValue;
				break;
			case ParamKind.CarrierRebate:
				_carrierRebate = reset ? null : (double)_paramValue;
				// Rebate перевозчика итог покупателя не меняет — сразу показываем, на что он влияет.
				_carrierView = true;
				break;
		}

		_param = null;
		await Recompute();
	}

	// --- цены тоннажей региона ---

	private string? RegionEditTitle => _regionEdit is { } edit ? T.Simulator.RegionEditTitle(T.Pricing.KgAsTons(edit.WeightKg), edit.Level == City) : null;

	private bool RegionEditHasOverride => _regionEdit is { } edit && _regionPrices.Any(x => x.Level == edit.Level && x.WeightKg == edit.WeightKg);

	private void OpenRegionPrice(uint weightKg, string level, PricingSimulationPrice? price)
	{
		_regionEdit = (weightKg, level);
		_regionMin = price?.Min ?? 0;
		_regionMax = price?.Max ?? 0;
		_regionError = null;
	}

	private void CloseRegionPrice() => _regionEdit = null;

	private async Task ApplyRegionPrice(bool reset)
	{
		if (_regionEdit is not { } edit)
		{
			return;
		}

		if (!reset && (_regionMin < 0 || _regionMax < _regionMin))
		{
			_regionError = T.Simulator.RegionPriceError;
			return;
		}

		_regionPrices.RemoveAll(x => x.Level == edit.Level && x.WeightKg == edit.WeightKg);
		if (!reset)
		{
			_regionPrices.Add(new PricingSimulationRegionPriceOverride(edit.Level, edit.WeightKg, _regionMin, _regionMax));
		}

		_regionEdit = null;
		await Recompute();
	}

	private async Task ClearRegionPrices()
	{
		_regionPrices.Clear();
		await Recompute();
	}

	// --- заказ из разных грузовиков ---

	private async Task AddDelivery(uint weight)
	{
		if (_custom.Count >= MaxCustomDeliveries)
		{
			return;
		}

		_custom.Add(weight);
		await Recompute();
	}

	private async Task RemoveDelivery(int index)
	{
		_custom.RemoveAt(index);
		await Recompute();
	}

	private async Task ClearDeliveries()
	{
		_custom.Clear();
		await Recompute();
	}
}
