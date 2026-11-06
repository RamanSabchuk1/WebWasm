using System.Globalization;

namespace WebWasm.Helpers;

/// <summary>
/// Чистая математика калькулятора цен: «круглые» деления осей, форматирование чисел, окно в слоте водителя,
/// склейка одинаковых ячеек таблицы и подпись VolumePriceProgression.
/// </summary>
public static class PriceChartMath
{
	/// <summary>Доля в процентах для подписи: 0.35 → «35 %».</summary>
	public static string Percent(double share) => $"{(share * 100).ToString("0", CultureInfo.InvariantCulture)} %";

	/// <summary>Изменение в процентах со знаком: −0.21 → «−21 %», 0.075 → «+7.5 %».</summary>
	public static string SignedPercent(decimal share) => $"{(share * 100).ToString("+0.#;−0.#;0", CultureInfo.InvariantCulture)} %";

	/// <summary>
	/// Что VolumePriceProgression делает с заказом. y — коэффициент применения (0 — значение действует полностью, 1 — никак),
	/// multiplier — 1 + значение × (1 − y) из backend, изменение доставки и комиссии = multiplier − 1. При значении 0 он ровно 1.
	/// Заказ из одной доставки ключ не меняет.
	/// </summary>
	public static string VolumeEffect(int deliveries, double y, decimal multiplier)
	{
		if (deliveries <= 1)
		{
			return "одна доставка, ключ не действует";
		}

		var applied = $"y {y.ToString("0.00", CultureInfo.InvariantCulture)}, применяется на {Percent(1 - y)}";
		var change = multiplier - 1m;
		return change == 0m ? $"{applied}, цены не меняются" : $"{applied}, доставка и комиссия {SignedPercent(change)}";
	}

	/// <summary>
	/// Сколько минут заказ займёт в слоте водителя, как в SlotHelper.GetStartAndEnd backend: начало = время заказа − подготовка,
	/// конец = время заказа + слот-время рейса; оба края округляются к ближайшим 5 минутам (доли минуты отбрасываются,
	/// остаток до 2 — вниз). Заказ считается на ровном времени (12:00).
	/// </summary>
	public static int SlotWindowMinutes(double preparationMinutes, double slotMinutes)
	{
		const int orderAt = 12 * 60;
		static int Round(double minuteOfDay)
		{
			var minute = (int)Math.Floor(minuteOfDay);
			var remainder = ((minute % 5) + 5) % 5;
			return remainder < 3 ? minute - remainder : minute + (5 - remainder);
		}

		return Round(orderAt + slotMinutes) - Round(orderAt - preparationMinutes);
	}

	/// <summary>
	/// Склейка соседних одинаковых ячеек строки: начало и длина каждого отрезка (colspan). Null склеивается с null.
	/// </summary>
	public static (int Start, int Length)[] Runs<T>(IReadOnlyList<T?> cells, Func<T, T, bool> same)
		where T : class
	{
		var runs = new List<(int, int)>();
		var start = 0;
		for (var i = 1; i <= cells.Count; i++)
		{
			if (i < cells.Count && Same(cells[start], cells[i], same))
			{
				continue;
			}

			runs.Add((start, i - start));
			start = i;
		}

		return [.. runs];
	}

	private static bool Same<T>(T? a, T? b, Func<T, T, bool> same)
		where T : class => a is null || b is null ? a is null && b is null : same(a, b);

	/// <summary>
	/// Деления оси от 0 до значения не меньше <paramref name="max"/> с шагом 1, 2, 2.5 или 5 × 10ⁿ,
	/// примерно <paramref name="targetTicks"/> делений (до трети больше). Max ≤ 0 → ось 0…1.
	/// </summary>
	public static (double Step, double Max, double[] Ticks) NiceAxis(double max, int targetTicks = 6)
	{
		if (max <= 0 || double.IsNaN(max) || double.IsInfinity(max))
		{
			max = 1;
		}

		var rough = max / Math.Max(1, targetTicks);
		var magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
		var step = new[] { 1, 2, 2.5, 5, 10 }.Select(x => x * magnitude).First(x => x >= rough * 0.75);
		var niceMax = Math.Ceiling(max / step) * step;
		var count = (int)Math.Round(niceMax / step);
		return (step, niceMax, [.. Enumerable.Range(0, count + 1).Select(i => Math.Round(i * step, 10))]);
	}

	/// <summary>Число для SVG-атрибута: точка как разделитель, один знак после запятой.</summary>
	public static string Svg(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

	/// <summary>Деньги для подписи: пробел между тысячами, без копеек от 1000 и выше.</summary>
	public static string Money(decimal value) =>
		(Math.Abs(value) >= 1000 ? value.ToString("#,0", CultureInfo.InvariantCulture) : value.ToString("#,0.##", CultureInfo.InvariantCulture))
			.Replace(',', ' ');

	/// <summary>Подпись деления оси: 1 200, 2.5, 0.75.</summary>
	public static string Tick(double value) =>
		value.ToString(Math.Abs(value) >= 1000 ? "#,0" : "0.##", CultureInfo.InvariantCulture).Replace(',', ' ');

	/// <summary>Килограммы в тонны для подписи: 10000 → «10 т», 2500 → «2.5 т».</summary>
	public static string Tons(double kilograms) => $"{(kilograms / 1000).ToString("0.##", CultureInfo.InvariantCulture)} т";
}
