using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace WebWasm.Components;

public partial class CustomPaginator : ComponentBase
{
	[Parameter] public PaginationState? State { get; set; }
	[Parameter] public int? TotalItems { get; set; }
	[Parameter] public Action? AfterNavigate { get; set; }

    private int? ResolvedTotalItems => TotalItems ?? State?.TotalItemCount;

	private int CurrentPage => State != null ? (State.CurrentPageIndex + 1) : 0;
	private int TotalPages => State != null && ResolvedTotalItems.HasValue
		? Math.Max(1, (int)Math.Ceiling((double)ResolvedTotalItems.Value / State.ItemsPerPage))
		: 0;

	private bool CanGoToPreviousPage() => State?.CurrentPageIndex > 0;
	private bool CanGoToNextPage() => State != null && ResolvedTotalItems.HasValue
		&& (State.CurrentPageIndex + 1) * State.ItemsPerPage < ResolvedTotalItems.Value;

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
}
