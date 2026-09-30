using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class VehiclesTable
{
	private const string SearchKey = "search_vehicles";
	private const string SortKey = "sort_vehicles";
	[Parameter]
	public required IEnumerable<Vehicle> Items { get; set; }

	[Parameter]
	public required EventCallback<Vehicle> OnDelete { get; set; }

	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private readonly HashSet<Guid> _expandedPhotos = [];
	private readonly HashSet<Guid> _expandedDrivers = [];

	private static readonly IReadOnlyDictionary<string, Func<Vehicle, object?>> _sortSelectors =
		new Dictionary<string, Func<Vehicle, object?>>
		{
			["model"] = v => v.Model,
			["registration"] = v => v.RegistrationNumber,
			["weight"] = v => v.VehicleWeight,
			["capacity"] = v => v.LoadCapacity,
		};

	private Vehicle[] FilteredVehicles
	{
		get
		{
			var filtered = Items.Where(v =>
				string.IsNullOrEmpty(_searchText) ||
				v.Model.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				v.RegistrationNumber.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				(v.Driver?.UserInfo?.FirstName ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				(v.Driver?.UserInfo?.LastName ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase)
			);

			return [.. SortHelper.Apply(filtered, _sortState, _sortSelectors)];
		}
	}

	private bool IsPhotoExpanded(Guid vehicleId) => _expandedPhotos.Contains(vehicleId);

	private void TogglePhotoExpand(Guid vehicleId) => _expandedPhotos.Toggle(vehicleId);

	private bool IsDriverExpanded(Guid vehicleId) => _expandedDrivers.Contains(vehicleId);

	private void ToggleDriverExpand(Guid vehicleId) => _expandedDrivers.Toggle(vehicleId);

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);

		_sortState = await LocalStorage.GetItemOrDefaultAsync(SortKey, new SortState());
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
}
