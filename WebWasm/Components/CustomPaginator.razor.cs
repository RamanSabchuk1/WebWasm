using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace WebWasm.Components;

public partial class CustomPaginator : ComponentBase, IDisposable
{
	[Parameter] public PaginationState? State { get; set; }
	[Parameter] public int? TotalItems { get; set; }
	[Parameter] public Action? AfterNavigate { get; set; }

	private PaginationState? _previousState;

	private int? ResolvedTotalItems => TotalItems ?? State?.TotalItemCount;

	private int CurrentPage => State != null ? (State.CurrentPageIndex + 1) : 0;
	private int TotalPages => State != null && ResolvedTotalItems.HasValue
		? Math.Max(1, (int)Math.Ceiling((double)ResolvedTotalItems.Value / State.ItemsPerPage))
		: 0;

	private bool CanGoToPreviousPage() => State?.CurrentPageIndex > 0;
	private bool CanGoToNextPage() => State != null && ResolvedTotalItems.HasValue
		&& (State.CurrentPageIndex + 1) * State.ItemsPerPage < ResolvedTotalItems.Value;

	protected override void OnParametersSet()
	{
		if (State != _previousState)
		{
			if (_previousState is not null)
			{
				_previousState.TotalItemCountChanged -= OnTotalItemCountChanged;
			}

			_previousState = State;

			if (State is not null)
			{
				State.TotalItemCountChanged += OnTotalItemCountChanged;
			}
		}
	}

	private void OnTotalItemCountChanged(object? sender, int? totalCount)
	{
		_ = InvokeAsync(StateHasChanged);
	}

	private async Task GoToFirstPage()
	{
		if (State != null && CanGoToPreviousPage())
		{
			await State.SetCurrentPageIndexAsync(0);
			AfterNavigate?.Invoke();
		}
	}

	private async Task GoToPreviousPage()
	{
		if (State != null && CanGoToPreviousPage())
		{
			await State.SetCurrentPageIndexAsync(State.CurrentPageIndex - 1);
			AfterNavigate?.Invoke();
		}
	}

	private async Task GoToNextPage()
	{
		if (State != null && CanGoToNextPage())
		{
			await State.SetCurrentPageIndexAsync(State.CurrentPageIndex + 1);
			AfterNavigate?.Invoke();
		}
	}

	private async Task GoToLastPage()
	{
		if (State != null && CanGoToNextPage())
		{
			var lastPage = TotalPages - 1;
			await State.SetCurrentPageIndexAsync(lastPage);
			AfterNavigate?.Invoke();
		}
	}

	public void Dispose()
	{
		if (_previousState is not null)
		{
			_previousState.TotalItemCountChanged -= OnTotalItemCountChanged;
		}
	}
}
