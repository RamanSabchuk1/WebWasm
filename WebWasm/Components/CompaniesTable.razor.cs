using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class CompaniesTable : ComponentBase
{
	private const string SearchKey = "search_companies";
	private const string SortKey = "sort_companies";
	private const string TypeFilterKey = "companies_type_filter";
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }
	[Parameter] public EventCallback<Company> OnEditCompany { get; set; }
	[Parameter] public EventCallback<Company> OnDeleteCompany { get; set; }
	[Parameter] public EventCallback<Company> OnEditSecurityLevel { get; set; }
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private CompanyType? _companyTypeFilter;
	private bool _showFilters;
	private readonly HashSet<Guid> _expandedCompanies = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private static readonly IReadOnlyDictionary<string, Func<Company, object?>> _sortSelectors =
		new Dictionary<string, Func<Company, object?>>
		{
			["email"] = c => c.CompanyInfo?.CorporateEmail ?? string.Empty,
			["created"] = c => c.Created,
		};

	private Company[] FilteredCompanies
	{
		get
		{
			IEnumerable<Company> filtered = Companies;

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				filtered = filtered.Where(c =>
					c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.UNP ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.CorporateEmail ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(c.CompanyInfo?.Address ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				);
			}

			if (_companyTypeFilter is { } typeFilter)
			{
				// Временно: API не присылает тип для покупателей (CompanyType.None == Buyer).
				filtered = filtered.Where(c =>
					(c.CompanyType & typeFilter) == typeFilter ||
					(typeFilter == CompanyType.Buyer && c.CompanyType == CompanyType.None));
			}

			return [.. SortHelper.Apply(filtered, _sortState, _sortSelectors)];
		}
	}

	private bool IsExpanded(Guid id) => _expandedCompanies.Contains(id);

	private void ToggleExpand(Guid id) => _expandedCompanies.Toggle(id);

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);

		_sortState = await LocalStorage.GetItemOrDefaultAsync(SortKey, new SortState());

		var typeFilter = await LocalStorage.GetItemOrDefaultAsync(TypeFilterKey, string.Empty);
		_companyTypeFilter = Enum.TryParse<CompanyType>(typeFilter, out var parsed) && parsed != CompanyType.None
			? parsed
			: null;
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		await LocalStorage.SetItemAsync(SortKey, _sortState);
	}

	private async Task SetTypeFilter(CompanyType? type)
	{
		_companyTypeFilter = type;
		await LocalStorage.SetItemAsync(TypeFilterKey, _companyTypeFilter?.ToString() ?? string.Empty);
	}

	private static IEnumerable<CompanyType> GetTypeFlags(CompanyType type)
	{
		// None не показываем: временно API не присылает тип для покупателей.
		foreach (var flag in new[] { CompanyType.Buyer, CompanyType.Cargo, CompanyType.Provider })
		{
			if ((type & flag) == flag)
			{
				yield return flag;
			}
		}
	}

	private static string FormatCreated(DateTime created) =>
		created == DateTime.MinValue ? string.Empty : created.ToString("d");
}
