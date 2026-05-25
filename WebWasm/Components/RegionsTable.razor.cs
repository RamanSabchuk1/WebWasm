using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class RegionsTable : ComponentBase
{
	private const string SearchKey = "search_regions";
	[Parameter] public List<Region> Regions { get; set; } = [];
	[Parameter] public EventCallback<Region> OnView { get; set; }
	[Parameter] public EventCallback<Region> OnEdit { get; set; }
	[Parameter] public EventCallback<Region> OnDelete { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private bool _hasItems => Regions.Count > 0;
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private List<Region> FilteredRegions
	{
		get
		{
			return string.IsNullOrWhiteSpace(_searchText)
				? Regions
				: [.. Regions.Where(r =>
					r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];
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
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}
}
