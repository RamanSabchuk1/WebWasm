using WebWasm.Helpers;

namespace WebWasm.Tests;

public class PriceChartMathTests
{
	[Theory]
	[InlineData(510, 10, 50, 550)]
	[InlineData(170, 10, 20, 180)]
	[InlineData(1234, 6, 200, 1400)]
	[InlineData(0.8, 6, 0.1, 0.8)]
	public void NiceAxis_RoundStepAndMax(double max, int ticks, double step, double niceMax)
	{
		var (Step, Max, Ticks) = PriceChartMath.NiceAxis(max, ticks);

		Assert.Equal(step, Step, 6);
		Assert.Equal(niceMax, Max, 6);
		Assert.Equal(0, Ticks[0]);
		Assert.Equal(niceMax, Ticks[^1], 6);
	}

	[Fact]
	public void NiceAxis_EmptyData_ZeroToOne()
	{
		Assert.Equal(1, PriceChartMath.NiceAxis(0).Max);
	}

	[Fact]
	public void Svg_IsCultureIndependent()
	{
		Assert.Equal("10.5", PriceChartMath.Svg(10.46));
	}

	// Как SlotHelper.GetStartAndEnd на backend: края окна округляются к ближайшим 5 минутам.
	[Theory]
	[InlineData(30, 52.8, 80)] // 11:30 … 12:52 → 12:50
	[InlineData(1, 52.8, 50)] // 11:59 → 12:00 … 12:50
	[InlineData(30, 60, 90)]
	[InlineData(0, 0, 0)]
	public void SlotWindow_RoundsEdgesToQuarterHour(double preparation, double slot, int expected)
	{
		Assert.Equal(expected, PriceChartMath.SlotWindowMinutes(preparation, slot));
	}

	[Fact]
	public void Runs_MergesEqualNeighbours()
	{
		string?[] cells = ["a", "a", "b", null, null, "a"];

		var runs = PriceChartMath.Runs(cells, (x, y) => x == y);

		Assert.Equal([(0, 2), (2, 1), (3, 2), (5, 1)], runs);
	}
}
