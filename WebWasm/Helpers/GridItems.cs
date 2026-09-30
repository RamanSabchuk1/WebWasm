using Microsoft.AspNetCore.Components.QuickGrid;

namespace WebWasm.Helpers;

/// <summary>
/// QuickGrid source for an in-memory list that is already filtered and sorted. Replaces <c>Items="list.AsQueryable()"</c>:
/// same paging, but no EnumerableQuery (it compiles expression trees and is not trim-safe).
/// </summary>
public static class GridItems
{
	public static GridItemsProvider<T> From<T>(IReadOnlyList<T> items) => request =>
	{
		var count = Math.Min(request.Count ?? items.Count, Math.Max(items.Count - request.StartIndex, 0));
		var page = new T[count];
		for (var i = 0; i < count; i++)
		{
			page[i] = items[request.StartIndex + i];
		}
		return ValueTask.FromResult(GridItemsProviderResult.From(page, items.Count));
	};
}
