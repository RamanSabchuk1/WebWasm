using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class Orders(CashService cashService, NavigationManager navigationManager)
{
	private int _totalOrders = 0;
	private int _pendingOrders = 0;
	private int _completedOrders = 0;
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredOrders.Any();
	private PaginationState _pagination = new() { ItemsPerPage = 10 };
	private Order[] _orders = [];

	private double? _minWeight;
	private double? _maxWeight;
	private double _weightRangeMin;
	private double _weightRangeMax;
	private double _weightStep = 1;
	private bool _weightFilterDisabled;
	private List<OrderStatus> _selectedStatuses = new();
	private bool _showFilters = false;

	private double CurrentMinWeight => _minWeight ?? _weightRangeMin;
	private double CurrentMaxWeight => _maxWeight ?? _weightRangeMax;

	private void OnMinWeightChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var value))
		{
			_minWeight = value;
			if (_minWeight > CurrentMaxWeight)
			{
				_maxWeight = _minWeight;
			}
		}
	}

	private void OnMaxWeightChanged(ChangeEventArgs e)
	{
		if (double.TryParse(e.Value?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var value))
		{
			_maxWeight = value;
			if (_maxWeight < CurrentMinWeight)
			{
				_minWeight = _maxWeight;
			}
		}
	}

	private void ClearWeightFilter()
	{
		_minWeight = null;
		_maxWeight = null;
	}

	private void RecalculateWeightRange()
	{
		if (_orders.Length == 0)
		{
			_weightRangeMin = 0;
			_weightRangeMax = 0;
			_weightStep = 1;
			_weightFilterDisabled = true;
			return;
		}

		_weightRangeMin = _orders.Min(o => o.TotalWeight);
		_weightRangeMax = _orders.Max(o => o.TotalWeight);
		var range = _weightRangeMax - _weightRangeMin;

		if (range <= 0)
		{
			_weightFilterDisabled = true;
			_weightStep = 1;
			return;
		}

		_weightFilterDisabled = false;

		var distinctCount = _orders.Select(o => o.TotalWeight).Distinct().Count();
		var stepCount = Math.Clamp(distinctCount - 1, 3, 10);
		_weightStep = range / stepCount;
	}

	private void AddStatus(ChangeEventArgs e)
	{
		if (Enum.TryParse<OrderStatus>(e.Value?.ToString(), out var status))
		{
			if (!_selectedStatuses.Contains(status))
			{
				_selectedStatuses.Add(status);
			}
		}
	}

	private void RemoveStatus(OrderStatus status)
	{
		_selectedStatuses.Remove(status);
	}

	private IQueryable<Order> FilteredOrders
	{
		get
		{
			var filtered = _orders.AsEnumerable();

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				filtered = filtered.Where(o =>
					o.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
					(o.Address != null && o.Address.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
					(o.State != null && o.State.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
				);
			}

			if (_selectedStatuses is not null && _selectedStatuses.Count > 0)
			{
				filtered = filtered.Where(o => _selectedStatuses.Contains(o.Status));
			}

			if (_minWeight.HasValue)
			{
				filtered = filtered.Where(o => o.TotalWeight >= _minWeight.Value);
			}

			if (_maxWeight.HasValue)
			{
				filtered = filtered.Where(o => o.TotalWeight <= _maxWeight.Value);
			}

			return filtered.AsQueryable();
		}
	}

	private async Task LoadData(bool useCash)
	{
		_orders = [.. await cashService.GetData<Order>(useCash)];
		_totalOrders = _orders.Length;
		_pendingOrders = _orders.Count(o => o.Status == OrderStatus.PaymentPending || o.Status == OrderStatus.WaitingApprove);
		_completedOrders = _orders.Count(o => o.Status == OrderStatus.Completed);
		RecalculateWeightRange();
	}

	private static string GetStatus(OrderStatus status)
	{
		return status switch
		{
			OrderStatus.Completed => "badge-completed",
			OrderStatus.Cancelled => "badge-cancelled",
			OrderStatus.Draft => "badge-draft",
			OrderStatus.WaitingApprove => "badge-waiting-approve",
			OrderStatus.PaymentPending => "badge-waiting-payment",
			OrderStatus.Active => "badge-active",
			OrderStatus.CorruptedPayment => "badge-corrupted",
			OrderStatus.Archived => "badge-archived",
			OrderStatus.Deleted => "badge-deleted",
			OrderStatus.PaymentInProgress=> "badge-progress",
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
