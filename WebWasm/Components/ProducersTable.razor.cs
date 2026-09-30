using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class ProducersTable : ComponentBase
{
	private const string SearchKey = "search_providers";
	[Parameter] public List<Producer> Producers { get; set; } = [];
	[Parameter] public EventCallback<Producer> OnEditProducer { get; set; }
	[Parameter] public EventCallback<Guid> OnDeleteProducer { get; set; }
	[Parameter] public EventCallback<Producer> OnAddLoadingPlace { get; set; }
	[Parameter] public EventCallback<(Guid ProducerId, LoadingPlace LoadingPlace)> OnEditLoadingPlace { get; set; }
	[Parameter] public EventCallback<(Guid ProducerId, Guid LoadingPlaceId)> OnDeleteLoadingPlace { get; set; }
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private string _searchText = string.Empty;
	private readonly HashSet<Guid> _expandedProducers = [];
	private readonly HashSet<Guid> _expandedWorkingTimes = [];
	private readonly HashSet<Guid> _expandedLoadingPlaces = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private IReadOnlyList<Producer> FilteredProducers
	{
		get
		{
			return string.IsNullOrWhiteSpace(_searchText)
				? Producers
				: [.. Producers.Where(p =>
					p.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					p.LoadingPlaces.Any(lp =>
						lp.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
						(lp.MaterialType != null && lp.MaterialType.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
				)];
		}
	}

	private bool IsProducerExpanded(Guid id) => _expandedProducers.Contains(id);
	private bool IsWorkingTimeExpanded(Guid id) => _expandedWorkingTimes.Contains(id);
	private bool IsLoadingPlacesExpanded(Guid id) => _expandedLoadingPlaces.Contains(id);

	private void ToggleProducerExpand(Guid id) => _expandedProducers.Toggle(id);
	private void ToggleWorkingTimeExpand(Guid id) => _expandedWorkingTimes.Toggle(id);
	private void ToggleLoadingPlacesExpand(Guid id) => _expandedLoadingPlaces.Toggle(id);

	private static string GetCompanyName(Producer producer)
	{
		return producer.Company?.Name ?? "(No Company)";
	}

	private static string GetDayAbbreviation(DayOfWeek day) => day switch
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

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}
}
