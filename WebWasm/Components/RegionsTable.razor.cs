using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class RegionsTable : ComponentBase
{
	private const string SearchKey = "search_regions";
	private const string SortKey = "sort_regions";
	[Parameter] public List<Region> Regions { get; set; } = [];
	[Parameter] public EventCallback<Region> OnView { get; set; }
	[Parameter] public EventCallback<Region> OnEdit { get; set; }
	[Parameter] public EventCallback<Region> OnDelete { get; set; }
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private bool _hasItems => Regions.Count > 0;
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private static readonly IReadOnlyDictionary<string, Func<Region, object?>> _sortSelectors =
		new Dictionary<string, Func<Region, object?>>
		{
			["name"] = r => r.Name,
		};

	private List<Region> FilteredRegions
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? Regions
				: [.. Regions.Where(r =>
					r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];

			return [.. SortHelper.Apply(filtered, _sortState, _sortSelectors)];
		}
	}

	private static List<string> GetRegionTypes(Region region)
	{
		var types = new HashSet<string>();
		if (region.Levels is not null)
		{
			foreach (var level in region.Levels)
			{
				types.Add(level.Type.ToString());
			}
		}

		return [.. types.OrderBy(t => t)];
	}

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);

		_sortState = await LocalStorage.GetItemOrDefaultAsync(SortKey, new SortState());
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		await LocalStorage.SetItemAsync(SortKey, _sortState);
	}
}
