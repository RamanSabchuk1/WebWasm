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

	// Confirmation dialog state
	private bool _showConfirmDialog = false;
	private string _confirmMessage = string.Empty;
	private Guid _pendingDeleteId = Guid.Empty;

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

	private void ShowDeleteConfirmation(MaterialType material)
	{
		_pendingDeleteId = material.Id;
		var hasChildren = Materials.Any(m => m.ParentId == material.Id);
		_confirmMessage = hasChildren 
			? $"Are you sure you want to delete '{material.Name}'? This will also delete all its child materials. This action cannot be undone."
			: $"Are you sure you want to delete '{material.Name}'? This action cannot be undone.";
		_showConfirmDialog = true;
	}

	private async Task ConfirmDelete()
	{
		_showConfirmDialog = false;
		await OnDelete.InvokeAsync(_pendingDeleteId);
		_pendingDeleteId = Guid.Empty;
	}

	private void CancelDelete()
	{
		_showConfirmDialog = false;
		_pendingDeleteId = Guid.Empty;
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
