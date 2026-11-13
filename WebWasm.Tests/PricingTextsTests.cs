using WebWasm.Localization;

namespace WebWasm.Tests;

/// <summary>Display formats of the price calculator in both languages (Texts.Pricing, Texts.Simulator).</summary>
public class PricingTextsTests
{
	private const string Nbsp = "\u00A0";

	[Fact]
	public void Money_NoKopecksForWholeAndLargeSums()
	{
		Assert.Equal($"12{Nbsp}346", Texts.Ru.Pricing.Money(12345.67m));
		Assert.Equal("99,50", Texts.Ru.Pricing.Money(99.5m));
		Assert.Equal("99.50", Texts.En.Pricing.Money(99.5m));
		Assert.Equal("40", Texts.Ru.Pricing.Money(40m));
	}

	[Fact]
	public void Tons_FromKilograms()
	{
		Assert.Equal($"2,5{Nbsp}т", Texts.Ru.Pricing.KgAsTons(2500));
		Assert.Equal($"2.5{Nbsp}t", Texts.En.Pricing.KgAsTons(2500));
		Assert.Equal($"10{Nbsp}т", Texts.Ru.Pricing.KgAsTons(10000));
	}

	[Theory]
	[InlineData(-0.21, "−21")]
	[InlineData(0.075, "+7,5")]
	[InlineData(0, "0")]
	public void SignedShare_HasSign(double share, string expected)
	{
		Assert.Equal($"{expected}{Nbsp}%", Texts.Ru.Pricing.SignedShare((decimal)share));
	}

	// VolumePriceProgression: y is the application factor, the change is multiplier − 1 (built like PricingSimulator.VolumeEffect).
	// The owner's example: y = 0.3, value −0.3 → −21 %.
	[Theory]
	[InlineData(1, 1.0, 1.0, "одна доставка, ключ не действует")]
	[InlineData(2, 0.3, 1.0, "y 0,30, применяется на 70 %, цены не меняются")]
	[InlineData(2, 0.3, 0.79, "y 0,30, применяется на 70 %, доставка и комиссия −21 %")]
	[InlineData(3, 0.92, 1.08, "y 0,92, применяется на 8 %, доставка и комиссия +8 %")]
	public void VolumeEffect_ShowsApplicationAndChange(int deliveries, double y, double multiplier, string expected)
	{
		var t = Texts.Ru;
		var change = (decimal)multiplier - 1m;

		var text = t.Simulator.VolumeEffect(deliveries, t.FormatNumber(y, 2), t.Pricing.Share(1 - y), change == 0m ? null : t.Pricing.SignedShare(change));

		Assert.Equal(expected.Replace(" %", Nbsp + "%"), text);
	}
}
