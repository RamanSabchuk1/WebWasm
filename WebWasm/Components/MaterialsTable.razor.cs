using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class MaterialsTable
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
	private readonly ConfirmState _confirm = new();
	// Children by parent id, built once per parameter change (was a scan of all materials per row).
	private ILookup<Guid?, MaterialType> _childrenByParent = Enumerable.Empty<MaterialType>().ToLookup(m => m.ParentId);

	protected override void OnParametersSet() => _childrenByParent = Materials.ToLookup(m => m.ParentId);

	/// <summary>Root materials matching the search; children are shown under their root.</summary>
	private List<MaterialType> FilteredMaterials
	{
		get
		{
			IEnumerable<MaterialType> filtered = string.IsNullOrWhiteSpace(_searchText)
				? Materials
				: Materials.Where(m =>
					m.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Description.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					m.Solidity.ToString().Contains(_searchText, StringComparison.OrdinalIgnoreCase));

			return [.. filtered.Where(m => m.ParentId == null)];
		}
	}

	private IEnumerable<MaterialType> PageOf(List<MaterialType> roots) =>
		roots.Skip(_pagination.CurrentPageIndex * _pagination.ItemsPerPage).Take(_pagination.ItemsPerPage);

	private void ToggleExpand(Guid materialId) => _expandedIds.Toggle(materialId);

	private void AskDelete(MaterialType material, bool hasChildren) => _confirm.Ask(
		T.Materials.DeleteTitle,
		hasChildren ? T.Materials.DeleteWithChildrenMessage(material.Name) : T.Materials.DeleteMessage(material.Name),
		() => OnDelete.InvokeAsync(material.Id));

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}
}
