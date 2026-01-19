using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Linq.Expressions;

namespace WebWasm.Components;

public partial class DataGrid<TItem> : ComponentBase
{
	[Parameter] public IQueryable<TItem>? ItemsQueryable { get; set; }
	[Parameter] public IEnumerable<TItem>? Items { get; set; }
	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public bool ShowSearch { get; set; } = true;
	[Parameter] public bool ShowPagination { get; set; } = true;
	[Parameter] public int PageSize { get; set; } = 10;
	[Parameter] public Func<TItem, string, bool>? SearchFilter { get; set; }

	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredItems.Any();

	protected override void OnParametersSet()
	{
		_pagination.ItemsPerPage = PageSize;
	}

	private IQueryable<TItem> FilteredItems
	{
		get
		{
			var items = ItemsQueryable ?? (Items?.AsQueryable() ?? Enumerable.Empty<TItem>().AsQueryable());

			if (!string.IsNullOrWhiteSpace(_searchText) && SearchFilter != null)
			{
				items = items.Where(item => SearchFilter(item, _searchText));
			}

			return items;
		}
	}
}
