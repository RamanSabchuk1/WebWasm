using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class CompaniesTable : ComponentBase
{
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }
    [Parameter] public EventCallback<Company> OnEditCompany { get; set; }

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
}
