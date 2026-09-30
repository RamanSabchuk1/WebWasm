using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class MaterialsTable : ComponentBase
{
	private const string SearchKey = "search_materials";
	[Parameter] public List<MaterialType> Materials { get; set; } = [];
	[Parameter] public EventCallback<MaterialType> OnEdit { get; set; }
	[Parameter] public EventCallback<Guid> OnDelete { get; set; }
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private bool _hasItems => Materials.Count > 0;
	private readonly HashSet<Guid> _expandedIds = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	// Children by parent id, built once per parameter change (was a scan of all materials per row).
	private ILookup<Guid?, MaterialType> _childrenByParent = Enumerable.Empty<MaterialType>().ToLookup(m => m.ParentId);

	protected override void OnParametersSet() => _childrenByParent = Materials.ToLookup(m => m.ParentId);

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
				: [.. Materials.Where(m =>
					m.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Solidity.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];

			// Only show root items (no parent) for pagination
			return [.. filtered.Where(m => m.ParentId == null)];
		}
	}

	private void ToggleExpand(Guid materialId) => _expandedIds.Toggle(materialId);

	private void ShowDeleteConfirmation(MaterialType material)
	{
		_pendingDeleteId = material.Id;
		var hasChildren = _childrenByParent[material.Id].Any();
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

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}

}
