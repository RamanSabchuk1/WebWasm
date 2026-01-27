using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;

namespace WebWasm.Components;

public partial class CustomPaginator : ComponentBase
{
	[Parameter] public PaginationState? State { get; set; }

	private int CurrentPage => State != null ? (State.CurrentPageIndex + 1) : 0;
	private int TotalPages => State != null && State.TotalItemCount.HasValue 
		? Math.Max(1, (int)Math.Ceiling((double)State.TotalItemCount.Value / State.ItemsPerPage)) 
		: 0;

	private bool CanGoToPreviousPage() => State?.CurrentPageIndex > 0;
	private bool CanGoToNextPage() => State != null && State.TotalItemCount.HasValue 
		&& (State.CurrentPageIndex + 1) * State.ItemsPerPage < State.TotalItemCount.Value;

	private async Task GoToFirstPage()
	{
		if (State != null && CanGoToPreviousPage())
		{
			await State.SetCurrentPageIndexAsync(0);
		}
	}

	private async Task GoToPreviousPage()
	{
		if (State != null && CanGoToPreviousPage())
		{
			await State.SetCurrentPageIndexAsync(State.CurrentPageIndex - 1);
		}
	}

	private async Task GoToNextPage()
	{
		if (State != null && CanGoToNextPage())
		{
			await State.SetCurrentPageIndexAsync(State.CurrentPageIndex + 1);
		}
	}

	private async Task GoToLastPage()
	{
		if (State != null && CanGoToNextPage())
		{
			var lastPage = TotalPages - 1;
			await State.SetCurrentPageIndexAsync(lastPage);
		}
	}

	/// <summary>
	/// Set the current page to a specific page number (1-based)
	/// </summary>
	/// <param name="pageNumber">The page number to navigate to (1-based)</param>
	public async Task SetPage(int pageNumber)
	{
		if (State != null && pageNumber > 0 && pageNumber <= TotalPages)
		{
			await State.SetCurrentPageIndexAsync(pageNumber - 1);
			StateHasChanged();
		}
	}
}
