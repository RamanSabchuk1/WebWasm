using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Diagnostics.CodeAnalysis;
using WebWasm.Helpers;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class CompaniesTable : ComponentBase
{
	private const string SearchKey = "search_companies";
	private const string SortKey = "sort_companies";
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }
	[Parameter] public EventCallback<Company> OnEditCompany { get; set; }
	[Parameter] public EventCallback<Company> OnDeleteCompany { get; set; }
	[Parameter] public EventCallback<Company> OnEditSecurityLevel { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private bool _hasItems => FilteredCompanies.Any();
	private readonly HashSet<Guid> _expandedCompanies = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private static readonly IReadOnlyDictionary<string, Func<Company, object?>> _sortSelectors =
		new Dictionary<string, Func<Company, object?>>
		{
			["email"] = c => c.CompanyInfo?.CorporateEmail ?? string.Empty,
		};

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

			return SortHelper.Apply(filtered, _sortState, _sortSelectors).AsQueryable();
		}
	}

	private bool IsExpanded(Guid id) => _expandedCompanies.Contains(id);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedCompanies.Remove(id))
		{
			_expandedCompanies.Add(id);
		}
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

	private async Task CycleSortEmail()
	{
		_sortState = SortHelper.Cycle(_sortState, "email");
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}
}
