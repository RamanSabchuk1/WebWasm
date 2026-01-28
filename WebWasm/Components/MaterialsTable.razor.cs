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
			return [.. filtered.Where(m => m.ParentId == null)];
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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Task.Delay(100);
			await _pagination.SetCurrentPageIndexAsync(0);
			StateHasChanged();
		}
	}
}
