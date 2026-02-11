using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class ProducersTable : ComponentBase
{
	[Parameter] public List<Producer> Producers { get; set; } = [];
	[Parameter] public List<Company> Companies { get; set; } = [];
	[Parameter] public EventCallback<Producer> OnEditProducer { get; set; }
	[Parameter] public EventCallback<Guid> OnDeleteProducer { get; set; }
	[Parameter] public EventCallback<Producer> OnAddLoadingPlace { get; set; }
	[Parameter] public EventCallback<(Guid ProducerId, LoadingPlace LoadingPlace)> OnEditLoadingPlace { get; set; }
	[Parameter] public EventCallback<(Guid ProducerId, Guid LoadingPlaceId)> OnDeleteLoadingPlace { get; set; }

	private string _searchText = string.Empty;
	private bool _hasItems => FilteredProducers.Any();
	private HashSet<Guid> _expandedProducers = [];
	private HashSet<Guid> _expandedWorkingTimes = [];
	private HashSet<Guid> _expandedLoadingPlaces = [];
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private IQueryable<Producer> FilteredProducers
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? Producers
				: Producers.Where(p =>
					p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					p.LoadingPlaces.Any(lp =>
						lp.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
						(lp.MaterialType != null && lp.MaterialType.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
				).ToList();

			return filtered.AsQueryable();
		}
	}

	private bool IsProducerExpanded(Guid id) => _expandedProducers.Contains(id);
	private bool IsWorkingTimeExpanded(Guid id) => _expandedWorkingTimes.Contains(id);
	private bool IsLoadingPlacesExpanded(Guid id) => _expandedLoadingPlaces.Contains(id);

	private void ToggleProducerExpand(Guid id)
	{
		if (!_expandedProducers.Remove(id))
			_expandedProducers.Add(id);
	}

	private void ToggleWorkingTimeExpand(Guid id)
	{
		if (!_expandedWorkingTimes.Remove(id))
			_expandedWorkingTimes.Add(id);
	}

	private void ToggleLoadingPlacesExpand(Guid id)
	{
		if (!_expandedLoadingPlaces.Remove(id))
			_expandedLoadingPlaces.Add(id);
	}

	private string GetCompanyName(Producer producer)
	{
		return producer.Company?.LegalName ?? "(No Company)";
	}

	private string GetDayAbbreviation(DayOfWeek day) => day switch
	{
		DayOfWeek.Monday => "Mon",
		DayOfWeek.Tuesday => "Tue",
		DayOfWeek.Wednesday => "Wed",
		DayOfWeek.Thursday => "Thu",
		DayOfWeek.Friday => "Fri",
		DayOfWeek.Saturday => "Sat",
		DayOfWeek.Sunday => "Sun",
		_ => day.ToString()
	};
}
