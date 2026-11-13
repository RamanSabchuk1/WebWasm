using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WebWasm.Models;
using static WebWasm.Helpers.PriceChartMath;

namespace WebWasm.Components;

/// <summary>
/// График калькулятора цен: X — доля пути по городу (0 — весь путь за городом, цена по км и таблице пригорода;
/// 100 % — весь по городу, цена по времени и таблице города), Y — цена. Линия = тоннаж грузовика × тип покупателя
/// при весе заказа с ползунка; цвет — тоннаж, пунктир — покупатель без компании, толстая со звёздами — заказ из разных грузовиков.
/// Подсказка при наведении (на телефоне — касанием) показывает значения видимых линий в этой точке, по убыванию.
/// Рисуется SVG без JS-библиотек; JS нужен только чтобы узнать размер и положение графика.
/// Цвета линий — CSS-классы pc-c0…pc-c6 и pc-custom (токены темы), элементы рисуются currentColor.
/// </summary>
public partial class PriceChart
{
	[Inject] private IJSRuntime JS { get; set; } = default!;

	[Parameter] public PricingSimulationResult? Result { get; set; }
	[Parameter] public bool HasCarrier { get; set; }

	/// <summary>Вид «после принятия перевозчиком»: доставка — сколько получит он, комиссия — что останется платформе.</summary>
	[Parameter] public bool CarrierView { get; set; }
	[Parameter] public EventCallback<bool> CarrierViewChanged { get; set; }

	/// <summary>Точка оси X, выбранная касанием или кликом (для разбивки заказа из разных грузовиков под графиком).</summary>
	[Parameter] public EventCallback<int> PointSelected { get; set; }

	private enum ViewMode
	{
		Total,
		Parts,
	}

	private const double Ml = 64;
	private const double Mr = 24;
	private const double Mt = 16;
	private const double Mb = 44;
	private const int MaxTipRows = 8;
	private const string CustomColor = "pc-custom";
	private const int PaletteSize = 7;

	private static readonly double[] XTicks = [0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1];

	private ViewMode _mode = ViewMode.Total;
	private bool _carrierViewShown;
	private bool _perTon;
	private bool _bands = true;

	private readonly HashSet<uint> _hiddenWeights = [];
	private readonly HashSet<bool> _hiddenBuyers = [];
	private readonly HashSet<string> _hiddenSeries = [];

	private ElementReference _container;
	private double _width = 900;
	private double _left;
	private double _top;
	private int? _hoverIndex;
	private int? _pinnedIndex;
	private double? _cursorY;

	/// <summary>Color — CSS-класс цвета линии.</summary>
	private sealed record SeriesView(string Key, PricingSimulationSeries Series, string Label, string Color, double StrokeWidth, string? Dash, bool Visible);

	private sealed record PathView(string Key, string D, string Color, double Width, string? Dash, double Opacity);

	private sealed record TextView(string Key, double X, double Y, string Text, string Color, string Anchor = "start");

	private sealed record StarView(string Key, double X, double Y);

	private sealed record TipRow(string Key, string Color, string Label, string Text, decimal Value, double Y, bool Highlight);

	private sealed class BoundingBox
	{
		public double Left { get; set; }
		public double Top { get; set; }
		public double Width { get; set; }
	}

	private List<SeriesView> _series = [];
	private List<PathView> _paths = [];
	private List<string> _bandPolygons = [];
	private List<string> _bandColors = [];
	private List<TextView> _labels = [];
	private List<StarView> _stars = [];
	private List<TipRow> _tipRows = [];
	private double[] _yTicks = [];
	private double _yMax = 1;

	private bool Narrow => _width < 600;
	private double Height => Narrow ? 320 : 440;
	private double PlotWidth => Math.Max(10, _width - Ml - Mr);
	private double PlotHeight => Height - Mt - Mb;

	private uint[] Weights => Result is null ? [] : [.. Result.Series.Select(x => x.WeightKg).Distinct().OrderBy(x => x == 0 ? uint.MaxValue : x)];
	private bool HasCarrierData => HasCarrier && Result?.Series.Any(s => s.Points.Any(p => p.CarrierDelivery is not null)) == true;
	private int? ActiveIndex => _hoverIndex ?? _pinnedIndex;

	protected override void OnParametersSet()
	{
		// Вид «после принятия перевозчиком» виден только в разбивке: его включение извне (правка rebate перевозчика) переключает вид.
		if (CarrierView && !_carrierViewShown)
		{
			_mode = ViewMode.Parts;
		}

		_carrierViewShown = CarrierView;
		Rebuild();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Measure();
			Rebuild();
			StateHasChanged();
		}
	}

	private async Task Measure()
	{
		try
		{
			if (await JS.InvokeAsync<BoundingBox?>("getElementCoordinates", _container) is { Width: > 0 } box)
			{
				_left = box.Left;
				_top = box.Top;
				_width = box.Width;
			}
		}
		catch (JSException)
		{
			// Без размеров график остаётся в ширине по умолчанию и просто масштабируется браузером.
		}
	}

	private string WeightName(uint weight) => weight == 0 ? T.PriceChart.CustomOrder : T.Pricing.KgAsTons(weight);

	private static string SeriesKey(PricingSimulationSeries s) => $"{s.WeightKg}|{s.BuyerHasCompany}";

	// Ключи элементов SVG уникальны среди всех детей <svg>: у полос, линий, подписей и звёзд свои префиксы.
	private static string BandKey(int index) => $"band|{index}";

	private string WeightColor(uint weight) => weight == 0
		? CustomColor
		: $"pc-c{Math.Max(0, Array.IndexOf(Weights, weight)) % PaletteSize}";

	private string SeriesLabel(PricingSimulationSeries s)
	{
		var trucks = s.WeightKg == 0
			? T.PriceChart.CustomSeries(s.Deliveries, T.Pricing.Tons(s.Tons))
			: $"{T.Pricing.KgAsTons(s.WeightKg)} × {s.Deliveries} = {T.Pricing.Tons(s.Tons)}";
		return $"{trucks} · {T.Pricing.Buyer(s.BuyerHasCompany)}";
	}

	private double TModeAt(int index) => Result?.Series.FirstOrDefault(s => s.Points.Length > index)?.Points[index].TMode ?? 0;

	private PricingSimulationPoint? PointAt(int index) =>
		_series.FirstOrDefault(s => s.Visible && s.Series.Points.Length > index)?.Series.Points[index]
		?? Result?.Series.FirstOrDefault(s => s.Points.Length > index)?.Points[index];

	private void Rebuild()
	{
		_paths = [];
		_bandPolygons = [];
		_bandColors = [];
		_labels = [];
		_stars = [];
		if (Result is null)
		{
			_series = [];
			_tipRows = [];
			return;
		}

		_series = [.. Result.Series
			.OrderBy(s => s.WeightKg == 0 ? uint.MaxValue : s.WeightKg)
			.ThenByDescending(s => s.BuyerHasCompany)
			.Select(s => new SeriesView(
				SeriesKey(s),
				s,
				SeriesLabel(s),
				WeightColor(s.WeightKg),
				s.WeightKg == 0 ? 3.2 : 2,
				s.BuyerHasCompany ? null : "7 4",
				!_hiddenWeights.Contains(s.WeightKg) && !_hiddenBuyers.Contains(s.BuyerHasCompany)
					&& !_hiddenSeries.Contains(SeriesKey(s)) && s.Points.Length > 0))];

		var visible = _series.Where(s => s.Visible).ToArray();
		var values = visible.SelectMany(s => s.Series.Points.SelectMany(p => PlottedValues(p, s.Series.Tons)));
		(_, _yMax, _yTicks) = NiceAxis((double)values.DefaultIfEmpty(1).Max(), 6);

		foreach (var s in visible)
		{
			AddSeries(s);
		}

		BuildTooltip();
	}

	private decimal PerTon(decimal value, double tons) => _perTon && tons > 0 ? value / (decimal)tons : value;

	private decimal DeliveryOf(PricingSimulationPoint p) => CarrierView && p.CarrierDelivery is { } d ? d : p.Delivery;

	private decimal CommissionOf(PricingSimulationPoint p) => CarrierView && p.CarrierCommission is { } c ? c : p.Commission;

	private IEnumerable<decimal> PlottedValues(PricingSimulationPoint p, double tons) => _mode switch
	{
		ViewMode.Parts => [PerTon(p.Total, tons), PerTon(p.Material, tons), PerTon(DeliveryOf(p), tons), PerTon(CommissionOf(p), tons)],
		_ when _bands => [PerTon(p.TotalHigh, tons), PerTon(p.Total, tons)],
		_ => [PerTon(p.Total, tons)],
	};

	private double X(double tMode) => Ml + (tMode * PlotWidth);

	private double Y(decimal value) => Mt + PlotHeight - ((double)value / _yMax * PlotHeight);

	private void AddSeries(SeriesView s)
	{
		var points = s.Series.Points;
		var tons = s.Series.Tons;
		switch (_mode)
		{
			case ViewMode.Total:
				if (_bands && s.Series.WeightKg != 0)
				{
					_bandPolygons.Add(Band(points, tons));
					_bandColors.Add(s.Color);
				}

				_paths.Add(new PathView($"path|{s.Key}|total", Polyline(points, p => PerTon(p.Total, tons)), s.Color, s.StrokeWidth, s.Dash, 1));
				break;

			case ViewMode.Parts:
				_paths.Add(new PathView($"path|{s.Key}|total", Polyline(points, p => PerTon(p.Total, tons)), s.Color, 1, null, 0.3));
				_paths.Add(new PathView($"path|{s.Key}|material", Polyline(points, p => PerTon(p.Material, tons)), s.Color, s.StrokeWidth, "2 4", 1));
				_paths.Add(new PathView($"path|{s.Key}|delivery", Polyline(points, p => PerTon(DeliveryOf(p), tons)), s.Color, s.StrokeWidth, "10 5", 1));
				_paths.Add(new PathView($"path|{s.Key}|commission", Polyline(points, p => PerTon(CommissionOf(p), tons)), s.Color, s.StrokeWidth, null, 1));
				var last = points[^1];
				_labels.Add(new TextView($"label|{s.Key}|material", X(last.TMode) - 4, Y(PerTon(last.Material, tons)) - 8, T.PriceChart.MaterialShort, s.Color, "end"));
				_labels.Add(new TextView($"label|{s.Key}|delivery", X(last.TMode) - 4, Y(PerTon(DeliveryOf(last), tons)) - 8, T.PriceChart.DeliveryShort, s.Color, "end"));
				_labels.Add(new TextView($"label|{s.Key}|commission", X(last.TMode) - 4, Y(PerTon(CommissionOf(last), tons)) - 8, T.PriceChart.CommissionShort, s.Color, "end"));
				break;
		}

		if (s.Series.WeightKg == 0)
		{
			for (var i = 0; i < points.Length; i += 2)
			{
				_stars.Add(new StarView($"star|{s.Key}|{i}", X(points[i].TMode), Y(PerTon(points[i].Total, tons))));
			}
		}
	}

	private string Polyline(IEnumerable<PricingSimulationPoint> points, Func<PricingSimulationPoint, decimal> value)
	{
		var sb = new StringBuilder();
		foreach (var p in points)
		{
			sb.Append(sb.Length == 0 ? 'M' : 'L').Append(Svg(X(p.TMode))).Append(',').Append(Svg(Y(value(p)))).Append(' ');
		}

		return sb.ToString();
	}

	private string Band(PricingSimulationPoint[] points, double tons)
	{
		var top = points.Select(p => $"{Svg(X(p.TMode))},{Svg(Y(PerTon(p.TotalHigh, tons)))}");
		var bottom = Enumerable.Reverse(points).Select(p => $"{Svg(X(p.TMode))},{Svg(Y(PerTon(p.TotalLow, tons)))}");
		return string.Join(' ', top.Concat(bottom));
	}

	private static string StarPath(double x, double y)
	{
		var sb = new StringBuilder();
		for (var i = 0; i < 10; i++)
		{
			var r = i % 2 == 0 ? 7 : 3;
			var a = (Math.PI / 5 * i) - (Math.PI / 2);
			sb.Append(i == 0 ? 'M' : 'L').Append(Svg(x + (r * Math.Cos(a)))).Append(',').Append(Svg(y + (r * Math.Sin(a)))).Append(' ');
		}

		return sb.Append('Z').ToString();
	}

	/// <summary>
	/// Строки подсказки: видимые линии в выбранной точке, по убыванию значения. Ближайшая к курсору выделена (и обведена на графике),
	/// показываются первые <see cref="MaxTipRows"/>, остальные — счётчиком, чтобы карточка не закрывала график.
	/// </summary>
	private void BuildTooltip()
	{
		_tipRows = [];
		if (ActiveIndex is not { } index)
		{
			return;
		}

		var unit = _perTon ? T.PriceChart.PerTonSuffix : string.Empty;
		var rows = new List<TipRow>();
		foreach (var s in _series.Where(s => s.Visible && s.Series.Points.Length > index))
		{
			var p = s.Series.Points[index];
			var tons = s.Series.Tons;
			var total = PerTon(p.Total, tons);
			var totalText = T.Pricing.Money(total) + unit;
			var text = _mode switch
			{
				ViewMode.Parts => T.PriceChart.TipParts(totalText, T.Pricing.Money(PerTon(p.Material, tons)),
					T.Pricing.Money(PerTon(DeliveryOf(p), tons)), T.Pricing.Money(PerTon(CommissionOf(p), tons))),
				_ => T.PriceChart.TipTotal(totalText, T.Pricing.Money(PerTon(p.TotalLow, tons)), T.Pricing.Money(PerTon(p.TotalHigh, tons))),
			};
			rows.Add(new TipRow(s.Key, s.Color, s.Label, text, total, Y(total), false));
		}

		rows.Sort((a, b) => b.Value.CompareTo(a.Value));
		if (_cursorY is { } cursor && rows.Count > 0)
		{
			var near = rows.MinBy(r => Math.Abs(r.Y - cursor))!;
			var at = rows.IndexOf(near);
			rows[at] = near with { Highlight = true };
			// Выделенная строка не должна прятаться за счётчиком «ещё N».
			if (at >= MaxTipRows)
			{
				rows.RemoveAt(at);
				rows.Insert(MaxTipRows - 1, near with { Highlight = true });
			}
		}

		_tipRows = rows;
	}

	private string TooltipSide(int index) => X(TModeAt(index)) > _width / 2 ? "left" : "right";

	private int? IndexAt(PointerEventArgs e)
	{
		var steps = Result?.Series.FirstOrDefault(s => s.Points.Length > 0)?.Points.Length ?? 0;
		if (steps == 0)
		{
			return null;
		}

		var share = Math.Clamp((e.ClientX - _left - Ml) / PlotWidth, 0, 1);
		return (int)Math.Round(share * (steps - 1));
	}

	private async Task OnPointerEnter(PointerEventArgs e)
	{
		await Measure();
		Rebuild();
		Hover(e);
	}

	private void OnPointerMove(PointerEventArgs e)
	{
		if (e.PointerType == "touch")
		{
			return;
		}

		Hover(e);
	}

	// Касание (телефон) закрепляет подсказку; клик мышью выбирает точку для разбивки заказа из разных грузовиков.
	private async Task OnPointerDown(PointerEventArgs e)
	{
		await Measure();
		var index = IndexAt(e);
		if (e.PointerType == "touch")
		{
			_pinnedIndex = _pinnedIndex == index ? null : index;
			_hoverIndex = null;
		}
		else
		{
			_hoverIndex = index;
		}

		_cursorY = e.ClientY - _top;
		BuildTooltip();
		if (index is { } selected)
		{
			await PointSelected.InvokeAsync(selected);
		}
	}

	private void OnPointerLeave()
	{
		_hoverIndex = null;
		_cursorY = null;
		BuildTooltip();
	}

	private void Hover(PointerEventArgs e)
	{
		_hoverIndex = IndexAt(e);
		_cursorY = e.ClientY - _top;
		BuildTooltip();
	}

	private void SetMode(ViewMode mode)
	{
		_mode = mode;
		Rebuild();
	}

	private void Toggle<TItem>(HashSet<TItem> set, TItem value)
	{
		if (!set.Remove(value))
		{
			set.Add(value);
		}

		Rebuild();
	}

	private void ShowAll()
	{
		_hiddenWeights.Clear();
		_hiddenBuyers.Clear();
		_hiddenSeries.Clear();
		Rebuild();
	}

	// Как в Grafana: клик по линии в легенде оставляет только её; повторный клик возвращает все.
	private void Isolate(SeriesView s)
	{
		var others = _series.Where(x => x.Key != s.Key).Select(x => x.Key).ToArray();
		if (others.All(_hiddenSeries.Contains) && !_hiddenSeries.Contains(s.Key))
		{
			_hiddenSeries.Clear();
		}
		else
		{
			_hiddenSeries.Clear();
			_hiddenSeries.UnionWith(others);
		}

		Rebuild();
	}

	private void OnPerTonChanged(ChangeEventArgs e)
	{
		_perTon = e.Value is true;
		Rebuild();
	}

	private void OnBandsChanged(ChangeEventArgs e)
	{
		_bands = e.Value is true;
		Rebuild();
	}

	private async Task OnCarrierViewChanged(ChangeEventArgs e) => await CarrierViewChanged.InvokeAsync(e.Value is true);
}
