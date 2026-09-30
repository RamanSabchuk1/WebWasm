using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Payments : ComponentBase
{
	private const string SearchKey = "search_payments";
	private const string SortKey = "sort_payments";
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private List<CreditCardInfo> _creditCards = [];
	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private static readonly IReadOnlyDictionary<string, Func<CreditCardInfo, object?>> _sortSelectors =
		new Dictionary<string, Func<CreditCardInfo, object?>>
		{
			["card"] = c => c.MaskedCard,
			["expiration"] = c => c.ExpirationDate,
			["unbind"] = c => c.UnbindAt,
		};

	private List<CreditCardInfo> FilteredCards
	{
		get
		{
			var filtered = string.IsNullOrWhiteSpace(_searchText)
				? _creditCards
				: [.. _creditCards.Where(c =>
					c.MaskedCard.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
				)];

			return [.. SortHelper.Apply(filtered, _sortState, _sortSelectors)];
		}
	}

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);

		_sortState = await LocalStorage.GetItemOrDefaultAsync(SortKey, new SortState());

		await LoadCreditCards(true);
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}

	private async Task LoadCreditCards(bool useCash)
	{
		_creditCards = [.. await CashService.GetData<CreditCardInfo>(useCash)];
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		await LocalStorage.SetItemAsync(SortKey, _sortState);
	}
}
