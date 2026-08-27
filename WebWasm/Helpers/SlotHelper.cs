using WebWasm.Models;

namespace WebWasm.Helpers;

public static class SlotHelper
{
	private const int PrepBuffer = 30;
	private const int ShiftCap = 60;
	private const int ShiftBase = 15;

	extension(DriverSlot[] slots)
	{
		public bool HasSlot(double duration, DateTime deliveryTime)
		{
			var orderDate = DateOnly.FromDateTime(deliveryTime);
			var time = TimeOnly.FromDateTime(deliveryTime);
			DriverSlot[] part = [.. slots.Where(x => x.WorkingDay == orderDate)];
			var (start, end) = deliveryTime.GetStartAndEnd(duration);
			return part.Any(slot => slot.IsSlotMatch(start, end));
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
		public (TimeOnly, TimeOnly) GetStartAndEnd(double duration)
		{
			var startDateTime = orderTime.AddMinutes(-PrepBuffer);
			var endDateTime = orderTime.AddMinutes(duration.CalculateOrderDuration());

			var start = TimeOnly.FromDateTime(startDateTime.RoundToNearest15Minutes());
			var end = TimeOnly.FromDateTime(endDateTime.RoundToNearest15Minutes());

			return (start, end);
		}

		public DateTime RoundToNearest15Minutes()
		{
			var minutes = orderTime.Minute;
			var remainder = minutes % 15;

			var roundedMinutes = remainder < 8
				? minutes - remainder
				: minutes + (15 - remainder);

			return new DateTime(orderTime.Year, orderTime.Month, orderTime.Day, orderTime.Hour, 0, 0).AddMinutes(roundedMinutes);
		}
	}

	extension(double minutes)
	{
		public double CalculateOrderDuration()
		{
			return minutes + minutes.CalculateDurationShift();
		}

		public double CalculateDurationShift()
		{
			return Math.Min(ShiftCap, minutes) + ShiftBase;
		}
	}
}
