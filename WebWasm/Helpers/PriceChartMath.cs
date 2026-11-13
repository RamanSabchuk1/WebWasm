using System.Globalization;

namespace WebWasm.Helpers;

/// <summary>
/// Чистая математика калькулятора цен: «круглые» деления осей, числа для SVG, окно в слоте водителя и
/// склейка одинаковых ячеек таблицы. Подписи для человека — в Localization/Texts.Pricing.cs (два языка).
/// </summary>
public static class PriceChartMath
{
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
}
