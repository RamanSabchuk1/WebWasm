using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class MaterialsTable : ComponentBase
{
	[Parameter] public List<MaterialType> Materials { get; set; } = [];
	[Parameter] public EventCallback<MaterialType> OnEdit { get; set; }
	[Parameter] public EventCallback<Guid> OnDelete { get; set; }

	private string _searchText = string.Empty;
	private bool _hasItems => Materials.Count > 0;
	private HashSet<Guid> _expandedIds = [];
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private List<MaterialType> FilteredMaterials
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? Materials
				: Materials.Where(m =>
					m.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Solidity.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				).ToList();

			// Only show root items (no parent) for pagination
			return filtered.Where(m => m.ParentId == null).ToList();
		}
	}

	private void ToggleExpand(Guid materialId)
	{
		if (_expandedIds.Contains(materialId))
		{
			_expandedIds.Remove(materialId);
		}
		else
		{
			_expandedIds.Add(materialId);
		}
	}

	protected override void OnAfterRender(bool firstRender)
	{
		// TotalItemCount is calculated internally by QuickGrid based on the IQueryable
		// We don't need to set it manually
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
