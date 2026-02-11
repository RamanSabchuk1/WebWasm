using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Orders(CashService cashService, NavigationManager navigationManager)
{
	private int _totalOrders = 0;
	private int _pendingOrders = 0;
	private int _completedOrders = 0;
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredOrders.Any();
	private PaginationState _pagination = new() { ItemsPerPage = 10 };
	private Order[] _orders = [];

	private IQueryable<Order> FilteredOrders
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? _orders
				: [.. _orders.Where(o =>
					o.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					o.Address.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					o.State.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];

			return filtered.AsQueryable();
		}
	}

	private async Task LoadData(bool useCash)
	{
		_orders = [.. await cashService.GetData<Order>(useCash)];
		_totalOrders = _orders.Length;
		_pendingOrders = _orders.Count(o => o.Status == OrderStatus.PaymentPending || o.Status == OrderStatus.WaitingApprove);
		_completedOrders = _orders.Count(o => o.Status == OrderStatus.Completed);
	}

	private static string GetStatus(OrderStatus status)
	{
		return status switch
		{
			OrderStatus.Completed => "badge-completed",
			OrderStatus.Cancelled => "badge-draft",
			OrderStatus.Draft => "badge-draft",
			OrderStatus.WaitingApprove => "badge-warning",
			OrderStatus.PaymentPending => "badge-warning",
			OrderStatus.Active => "badge-progress",
			OrderStatus.CorruptedPayment => "badge-deleted",
			OrderStatus.Archived => "badge-draft",
			OrderStatus.Deleted => "badge-deleted",
			_ => string.Empty
		};
	}

	private void NavigateToOrder(Guid id)
	{
		navigationManager.NavigateTo($"orders/{id}");
	}

	protected override async Task OnInitializedAsync()
	{
		await LoadData(true);
	}
}
