using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Diagnostics.CodeAnalysis;
using WebWasm.Helpers;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class RegionsTable : ComponentBase
{
	private const string SearchKey = "search_regions";
	private const string SortKey = "sort_regions";
	[Parameter] public List<Region> Regions { get; set; } = [];
	[Parameter] public EventCallback<Region> OnView { get; set; }
	[Parameter] public EventCallback<Region> OnEdit { get; set; }
	[Parameter] public EventCallback<Region> OnDelete { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

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
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }

		try { _sortState = await LocalStorage.GetItemAsync<SortState>(SortKey) ?? new SortState(); }
		catch { _sortState = new SortState(); }
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}
}
