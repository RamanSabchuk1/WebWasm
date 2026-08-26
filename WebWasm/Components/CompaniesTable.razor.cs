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
	private const string TypeFilterKey = "companies_type_filter";
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }
	[Parameter] public EventCallback<Company> OnEditCompany { get; set; }
	[Parameter] public EventCallback<Company> OnDeleteCompany { get; set; }
	[Parameter] public EventCallback<Company> OnEditSecurityLevel { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private CompanyType? _companyTypeFilter;
	private bool _hasItems => FilteredCompanies.Any();
	private readonly HashSet<Guid> _expandedCompanies = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private static readonly IReadOnlyDictionary<string, Func<Company, object?>> _sortSelectors =
		new Dictionary<string, Func<Company, object?>>
		{
			["email"] = c => c.CompanyInfo?.CorporateEmail ?? string.Empty,
			["created"] = c => c.Created,
		};

	private IQueryable<Company> FilteredCompanies
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
				// None = «без типа» (нет ни Vehicles, ни Producers); остальные — HasFlag (флаги комбинируются).
				filtered = typeFilter == CompanyType.None
					? filtered.Where(c => c.CompanyType == CompanyType.None)
					: filtered.Where(c => (c.CompanyType & typeFilter) == typeFilter);
			}

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

		try
		{
			var typeFilter = await LocalStorage.GetItemAsync<string>(TypeFilterKey);
			_companyTypeFilter = Enum.TryParse<CompanyType>(typeFilter, out var parsed) ? parsed : null;
		}
		catch { _companyTypeFilter = null; }
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}

	private async Task OnTypeFilterChanged(ChangeEventArgs e)
	{
		_companyTypeFilter = Enum.TryParse<CompanyType>(e.Value?.ToString(), out var parsed) ? parsed : null;
		try { await LocalStorage.SetItemAsync(TypeFilterKey, _companyTypeFilter?.ToString() ?? string.Empty); } catch { }
	}

	private static IEnumerable<CompanyType> GetTypeFlags(CompanyType type)
	{
		if (type == CompanyType.None)
		{
			yield return CompanyType.None;
			yield break;
		}

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
