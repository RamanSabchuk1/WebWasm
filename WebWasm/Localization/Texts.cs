using System.Globalization;

namespace WebWasm.Localization;

public enum Language
{
	Ru,
	En,
}

/// <summary>
/// All interface texts and display formats of one language. Components get it as the cascading parameter
/// <see cref="LocalizedComponent.T"/>; switching the language cascades the other instance and the whole UI re-renders.
/// Areas live in partial files <c>Texts.&lt;Area&gt;.cs</c>: <c>public XTexts X => field ??= new(Language);</c>.
/// </summary>
/// <remarks>
/// Not .resx: the app is built with InvariantGlobalization (download size, D-ww-4), so cultures and satellite assemblies
/// are not available. Both languages sit next to each other in one line, a missing translation does not compile.
/// Formats are explicit (FormatNumber, FormatDate…), CultureInfo.CurrentCulture stays invariant: URLs and parsing do not change.
/// </remarks>
public sealed partial class Texts
{
	public static readonly Texts Ru = new(Language.Ru);
	public static readonly Texts En = new(Language.En);

	private readonly NumberFormatInfo _numbers;
	private readonly string _datePattern;

	private Texts(Language language)
	{
		Language = language;
		_numbers = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
		if (language == Language.Ru)
		{
			_numbers.NumberGroupSeparator = "\u00A0";
			_numbers.NumberDecimalSeparator = ",";
		}

		_datePattern = language == Language.Ru ? "dd.MM.yyyy" : "dd MMM yyyy";
	}

	public Language Language { get; }

	/// <summary>Value for the html lang attribute and the language switch.</summary>
	public string Code => Language == Language.Ru ? "ru" : "en";

	public static Texts For(Language language) => language == Language.Ru ? Ru : En;

	public string FormatNumber(decimal value, int decimals = 2) => value.ToString("N" + decimals, _numbers);

	public string FormatNumber(double value, int decimals = 2) => value.ToString("N" + decimals, _numbers);

	public string FormatNumber(decimal? value, int decimals = 2) => value is { } v ? FormatNumber(v, decimals) : Dash;

	public string FormatNumber(double? value, int decimals = 2) => value is { } v ? FormatNumber(v, decimals) : Dash;

	/// <summary>Sum with the currency code after it: 1 234,50 BYN.</summary>
	public string FormatMoney(decimal value) => FormatNumber(value) + "\u00A0BYN";

	public string FormatMoney(decimal? value) => value is { } v ? FormatMoney(v) : Dash;

	public string FormatPercent(decimal value, int decimals = 1) => FormatNumber(value, decimals) + "\u00A0%";

	public string FormatDate(DateTime value) => value.ToString(_datePattern, DateTimeFormatInfo.InvariantInfo);

	public string FormatDate(DateOnly value) => value.ToString(_datePattern, DateTimeFormatInfo.InvariantInfo);

	public string FormatDate(DateTime? value) => value is { } v ? FormatDate(v) : Dash;

	public string FormatDateTime(DateTime value) => value.ToString(_datePattern + " HH:mm", DateTimeFormatInfo.InvariantInfo);

	public string FormatDateTime(DateTime? value) => value is { } v ? FormatDateTime(v) : Dash;

	public static string FormatTime(DateTime value) => value.ToString("HH:mm", DateTimeFormatInfo.InvariantInfo);

	public static string FormatTime(TimeOnly value) => value.ToString("HH:mm", DateTimeFormatInfo.InvariantInfo);

	/// <summary>Shown instead of an empty value.</summary>
	public const string Dash = "—";
}

/// <summary>Base of every area: <c>T("по-русски", "in English")</c>.</summary>
public abstract class TextBase(Language language)
{
	private readonly bool _ru = language == Language.Ru;

	protected string T(string ru, string en) => _ru ? ru : en;
}
