using WebWasm.Models;

namespace WebWasm.Helpers;

/// <summary>
/// Параметры слот-времени — те же, что backend берёт на AcceptOrder (глобальный резолв настроек ценообразования):
/// slotMinutes = duration × RoundTripFactor × (1 + TrafficBuffer) + LoadingMinutes, начало слота — за
/// PreparaionTimeSlotBuffer минут до заказа. Дефолты совпадают с реестром ключей backend.
/// </summary>
public sealed record SlotTimeParams(double RoundTripFactor, double TrafficBuffer, double LoadingMinutes, double PreparationBuffer)
{
	public static readonly SlotTimeParams Default = new(2, 0.30, 45, 30);

	/// <summary>Эффективные значения из GET admin/pricing-settings?scope=Global; нет ключа — дефолт.</summary>
	public static SlotTimeParams From(IEnumerable<PricingSettingItem> settings)
	{
		var values = settings.ToDictionary(x => x.Key, x => x.EffectiveValue ?? x.Default);
		double Value(string key, double fallback) => values.GetValueOrDefault(key) is { } v ? (double)v : fallback;

		return new SlotTimeParams(
			Value("RoundTripFactor", Default.RoundTripFactor),
			Value("TrafficBuffer", Default.TrafficBuffer),
			Value("LoadingMinutes", Default.LoadingMinutes),
			Value("PreparaionTimeSlotBuffer", Default.PreparationBuffer));
	}
}

public static class SlotHelper
{
	extension(DriverSlot[] slots)
	{
		public bool HasSlot(double duration, DateTime deliveryTime, SlotTimeParams slotTimeParams)
		{
			var orderDate = DateOnly.FromDateTime(deliveryTime);
			var (start, end) = deliveryTime.GetStartAndEnd(duration, slotTimeParams);
			return slots.Any(slot => slot.WorkingDay == orderDate && slot.IsSlotMatch(start, end));
		}
	}

	extension(DriverSlot slot)
	{
		public bool IsSlotMatch(TimeOnly start, TimeOnly end)
		{
			return slot.StartTime <= start && slot.EndTime >= end;
		}
	}

	extension(DateTime orderTime)
	{
		public (TimeOnly, TimeOnly) GetStartAndEnd(double duration, SlotTimeParams p)
		{
			var startDateTime = orderTime.AddMinutes(-p.PreparationBuffer);
			var endDateTime = orderTime.AddMinutes((duration * p.RoundTripFactor * (1 + p.TrafficBuffer)) + p.LoadingMinutes);

			var start = TimeOnly.FromDateTime(startDateTime.RoundToNearest5Minutes());
			var end = TimeOnly.FromDateTime(endDateTime.RoundToNearest5Minutes());

			return (start, end);
		}

		public DateTime RoundToNearest5Minutes()
		{
			var minutes = orderTime.Minute;
			var remainder = minutes % 5;

			var roundedMinutes = remainder < 3
				? minutes - remainder
				: minutes + (5 - remainder);

			return new DateTime(orderTime.Year, orderTime.Month, orderTime.Day, orderTime.Hour, 0, 0).AddMinutes(roundedMinutes);
		}
	}
}
