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
	private bool _hasItems => FilteredCards.Count != 0;
	private PaginationState _pagination = new() { ItemsPerPage = 10 };

	private List<CreditCardInfo> FilteredCards
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? _creditCards
				: [.. _creditCards.Where(c =>
					c.MaskedCard.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];

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
}
