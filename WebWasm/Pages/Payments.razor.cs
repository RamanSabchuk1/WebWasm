using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Payments : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<CreditCardInfo> _creditCards = [];
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredCards.Any();
	private bool _showConfirmDialog = false;
	private string _confirmMessage = string.Empty;
	private Guid _pendingDeleteId = Guid.Empty;
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private List<CreditCardInfo> FilteredCards
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? _creditCards
				: _creditCards.Where(c =>
					c.MaskedCard.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				).ToList();

			return filtered;
		}
	}

	protected override async Task OnInitializedAsync()
	{
		await LoadCreditCards(true);
	}

	private async Task LoadCreditCards(bool useCash)
	{
		_creditCards = [.. await CashService.GetData<CreditCardInfo>(useCash)];
	}

	private void ShowDeleteConfirmation(CreditCardInfo card)
	{
		_pendingDeleteId = card.Id;
		_confirmMessage = $"Are you sure you want to delete the credit card '{card.MaskedCard}'? This action cannot be undone.";
		_showConfirmDialog = true;
	}

	private async Task ConfirmDelete()
	{
		_showConfirmDialog = false;
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete($"Payments/{_pendingDeleteId}");
				ToastService.ShowSuccess("Credit card deleted successfully!");
				await LoadCreditCards(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete credit card: {ex.Message}");
			}
		});
		_pendingDeleteId = Guid.Empty;
	}

	private void CancelDelete()
	{
		_showConfirmDialog = false;
		_pendingDeleteId = Guid.Empty;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Task.Delay(100);
			await _pagination.SetCurrentPageIndexAsync(0);
			StateHasChanged();
		}
	}
}
