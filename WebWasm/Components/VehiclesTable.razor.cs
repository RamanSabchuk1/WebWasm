using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Diagnostics.CodeAnalysis;
using WebWasm.Helpers;
using WebWasm.Models;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class VehiclesTable
{
	private const string SearchKey = "search_vehicles";
	private const string SortKey = "sort_vehicles";
	[Parameter]
	public required IEnumerable<Vehicle> Items { get; set; }

	[Parameter]
	public required EventCallback<Vehicle> OnDelete { get; set; }

	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private readonly HashSet<Guid> _expandedPhotos = [];
	private readonly HashSet<Guid> _expandedDrivers = [];

	private bool HasItems => FilteredVehicles.Any();

	private static readonly IReadOnlyDictionary<string, Func<Vehicle, object?>> _sortSelectors =
		new Dictionary<string, Func<Vehicle, object?>>
		{
			["model"] = v => v.Model,
			["registration"] = v => v.RegistrationNumber,
			["weight"] = v => v.VehicleWeight,
			["capacity"] = v => v.LoadCapacity,
		};

	private IQueryable<Vehicle> FilteredVehicles
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

			return SortHelper.Apply(filtered, _sortState, _sortSelectors).AsQueryable();
		}
	}

	private bool IsPhotoExpanded(Guid vehicleId) => _expandedPhotos.Contains(vehicleId);

	private void TogglePhotoExpand(Guid vehicleId)
	{
		if (_expandedPhotos.Contains(vehicleId))
		{
			_expandedPhotos.Remove(vehicleId);
		}
		else
		{
			_expandedPhotos.Add(vehicleId);
		}
	}

	private bool IsDriverExpanded(Guid vehicleId) => _expandedDrivers.Contains(vehicleId);

	private void ToggleDriverExpand(Guid vehicleId)
	{
		if (_expandedDrivers.Contains(vehicleId))
		{
			_expandedDrivers.Remove(vehicleId);
		}
		else
		{
			_expandedDrivers.Add(vehicleId);
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

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}
}
