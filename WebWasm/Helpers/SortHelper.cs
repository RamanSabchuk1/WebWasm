using WebWasm.Models;

namespace WebWasm.Helpers;

/// <summary>
/// Shared three-state sort logic (None → Asc → Desc → None) for all QuickGrid tables.
/// Sort is applied externally (before passing items to QuickGrid), not via QuickGrid's internal sort.
/// </summary>
public static class SortHelper
{
	/// <summary>
	/// Cycles the sort state when a column header is clicked.
	/// Same column: None → Asc → Desc → None. Different column: starts at Asc.
	/// </summary>
	public static SortState Cycle(SortState current, string columnKey)
	{
		if (current.ColumnKey != columnKey)
		{
			// Clicked a different column → start ascending
			return new SortState(columnKey, Descending: false);
		}

		// Same column → cycle
		if (current.ColumnKey is null)
		{
			// Was None → Asc
			return new SortState(columnKey, Descending: false);
		}

		if (!current.Descending)
		{
			// Was Asc → Desc
			return current with { Descending = true };
		}

		// Was Desc → None
		return new SortState(null, false);
	}

	/// <summary>
	/// Applies the sort state to a sequence. Returns the sequence unchanged if state is None.
	/// </summary>
	/// <typeparam name="T">Item type.</typeparam>
	/// <param name="items">The filtered (but not yet sorted) items.</param>
	/// <param name="state">Current sort state.</param>
	/// <param name="keySelectors">Map of columnKey → key selector lambda.</param>
	public static IEnumerable<T> Apply<T>(
		IEnumerable<T> items,
		SortState state,
		IReadOnlyDictionary<string, Func<T, object?>> keySelectors)
	{
		if (state.ColumnKey is null || !keySelectors.TryGetValue(state.ColumnKey, out var selector))
		{
			return items;
		}

		return state.Descending
			? items.OrderByDescending(selector)
			: items.OrderBy(selector);
	}

	/// <summary>
	/// Returns the sort indicator glyph for a column: ▲ (asc), ▼ (desc), or empty (none).
	/// </summary>
	public static string GetIndicator(SortState state, string columnKey)
	{
		if (state.ColumnKey != columnKey)
		{
			return string.Empty;
		}

		return state.Descending ? "▼" : "▲";
	}

	/// <summary>
	/// Returns the CSS class for the sort header of a column.
	/// </summary>
	public static string GetHeaderClass(SortState state, string columnKey)
	{
		if (state.ColumnKey != columnKey)
		{
			return "sort-header";
		}

		return state.Descending ? "sort-header sort-desc" : "sort-header sort-asc";
	}
}