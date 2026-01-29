using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class RegionsTable : ComponentBase
{
	[Parameter] public List<Region> Regions { get; set; } = [];
	[Parameter] public EventCallback<Region> OnView { get; set; }
	[Parameter] public EventCallback<Region> OnEdit { get; set; }
	[Parameter] public EventCallback<Region> OnDelete { get; set; }

	private string _searchText = string.Empty;
	private bool _hasItems => Regions.Count > 0;
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private List<Region> FilteredRegions
	{
		get
		{
			return string.IsNullOrWhiteSpace(_searchText)
				? Regions
				: Regions.Where(r =>
					r.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				).ToList();
		}
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Task.Delay(100);
			await _pagination.SetCurrentPageIndexAsync(0);
			StateHasChanged();
		}
	}

	private List<string> GetRegionTypes(Region region)
	{
		var types = new HashSet<string>();
		if (region.Levels is not null)
		{
			foreach (var level in region.Levels)
			{
				types.Add(level.Type.ToString());
			}
		}
		return types.OrderBy(t => t).ToList();
	}

	public async Task SetPage(int pageNumber)
	{
		if (pageNumber > 0)
		{
			var maxPage = _pagination.TotalItemCount.HasValue
				? (int)Math.Ceiling((double)_pagination.TotalItemCount.Value / _pagination.ItemsPerPage)
				: 1;

			if (pageNumber <= maxPage)
			{
				await _pagination.SetCurrentPageIndexAsync(pageNumber - 1);
			}
		}
	}
}
