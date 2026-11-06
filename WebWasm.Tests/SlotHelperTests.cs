using WebWasm.Helpers;
using WebWasm.Models;

namespace WebWasm.Tests;

/// <summary>
/// Окно слота водителя должно совпадать с backend (Kliffort.Contracts/Helpers/SlotHelper.GetStartAndEnd):
/// иначе админка показывает водителя доступным, а backend отказывает в принятии, или наоборот.
/// Ожидания взяты из backend-тестов SlotHelperTests: заказ в 10:00, по умолчанию начало 09:30.
/// </summary>
public class SlotHelperTests
{
	private static readonly DateTime OrderTime = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

	// slot = d × 2 × 1.3 + 45; конец округляется к 5 минутам. d=60 → 201 мин → 13:21 → 13:20.
	[Theory]
	[InlineData(0, 10, 45)]
	[InlineData(30, 12, 5)]
	[InlineData(60, 13, 20)]
	[InlineData(120, 15, 55)]
	public void GetStartAndEnd_Defaults_MatchBackend(double duration, int endHour, int endMinute)
	{
		var (start, end) = OrderTime.GetStartAndEnd(duration, SlotTimeParams.Default);

		Assert.Equal(new TimeOnly(9, 30), start);
		Assert.Equal(new TimeOnly(endHour, endMinute), end);
	}

	// Значения из настроек ценообразования перекрывают дефолты, отсутствующий ключ берёт дефолт ключа.
	[Fact]
	public void From_UsesEffectiveValues()
	{
		PricingSettingItem Item(string key, decimal? effective, decimal? def) => new(key, "", ["Global"], def, null, effective, null, null);

		var p = SlotTimeParams.From(
		[
			Item("RoundTripFactor", 1m, 2m),
			Item("TrafficBuffer", 0m, 0.3m),
			Item("LoadingMinutes", null, 30m),
			Item("PreparaionTimeSlotBuffer", 60m, 30m),
		]);

		var (start, end) = OrderTime.GetStartAndEnd(60, p);

		Assert.Equal(new TimeOnly(9, 0), start);
		Assert.Equal(new TimeOnly(11, 30), end);
	}

	[Fact]
	public void HasSlot_OnlyWholeWindowOnSameDay()
	{
		var day = DateOnly.FromDateTime(OrderTime);
		DriverSlot Slot(int fromHour, int toHour, DateOnly workingDay) => new(Guid.NewGuid(), Guid.NewGuid(), new TimeOnly(fromHour, 30), new TimeOnly(toHour, 20), workingDay);

		Assert.True(new[] { Slot(9, 13, day) }.HasSlot(60, OrderTime, SlotTimeParams.Default));
		Assert.False(new[] { Slot(9, 12, day) }.HasSlot(60, OrderTime, SlotTimeParams.Default));
		Assert.False(new[] { Slot(9, 13, day.AddDays(1)) }.HasSlot(60, OrderTime, SlotTimeParams.Default));
	}
}
