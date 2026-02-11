using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class CompaniesTable : ComponentBase
{
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<(Guid CompanyId, bool IsActive)> OnToggleActive { get; set; }

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
					c.LegalName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					c.UNP.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					c.CorporateEmail.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					c.LegalAddress.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
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

	private string FormatTimeSpan(TimeSpan timeSpan)
	{
		return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}";
	}
}
