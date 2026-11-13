namespace WebWasm.Helpers;

public static class Aria
{
	/// <summary>
	/// Value of a state attribute (aria-pressed, aria-expanded). Blazor renders a bool attribute as an empty value
	/// or drops it, so CSS selectors like [aria-pressed="true"] and screen readers need the explicit string.
	/// </summary>
	public static string State(bool value) => value ? "true" : "false";
}
