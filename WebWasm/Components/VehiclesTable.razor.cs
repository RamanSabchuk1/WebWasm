using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class VehiclesTable
{
	[Parameter]
	public required IEnumerable<Vehicle> Items { get; set; }

	[Parameter]
	public required EventCallback<Vehicle> OnDelete { get; set; }

	private PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private readonly HashSet<Guid> _expandedPhotos = [];
	private readonly HashSet<Guid> _expandedDrivers = [];

	private bool _hasItems = false;

	protected override async Task OnInitializedAsync()
	{
		await ResetPagination();
	}

	private IQueryable<Vehicle> FilteredVehicles
	{
		get
		{
			return Items.Where(v =>
				string.IsNullOrEmpty(_searchText) ||
				v.Model.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				v.RegistrationNumber.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				(v.Driver?.UserInfo?.FirstName ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
				(v.Driver?.UserInfo?.LastName ?? "").Contains(_searchText, StringComparison.OrdinalIgnoreCase)
			).AsQueryable();
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

	private async Task ResetPagination()
	{
		var itemCount = Items.Count();
		_hasItems = itemCount > 0;
		_pagination.ItemsPerPage = 10;
		await Task.CompletedTask;
	}
}
