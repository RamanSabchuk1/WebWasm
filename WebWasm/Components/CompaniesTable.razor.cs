using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class CompaniesTable : ComponentBase
{
	private const string SearchKey = "search_companies";
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }
    [Parameter] public EventCallback<Company> OnEditCompany { get; set; }
    [Parameter] public EventCallback<Company> OnDeleteCompany { get; set; }
    [Parameter] public EventCallback<Company> OnEditSecurityLevel { get; set; }
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

    private string _searchText = string.Empty;
	private bool _hasItems => FilteredCompanies.Any();
	private HashSet<Guid> _expandedCompanies = [];
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private IQueryable<Company> FilteredCompanies
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? Companies
				: Companies.Where(c =>
					c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.UNP ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.CorporateEmail ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.Address ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				).ToList();

			return filtered.AsQueryable();
		}
	}

	private bool IsExpanded(Guid id) => _expandedCompanies.Contains(id);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedCompanies.Remove(id))
			_expandedCompanies.Add(id);
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
