using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Diagnostics.CodeAnalysis;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class Payments : ComponentBase
{
	private const string SearchKey = "search_payments";
	private const string SortKey = "sort_payments";
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private List<CreditCardInfo> _creditCards = [];
	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private bool _hasItems => FilteredCards.Count != 0;
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
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }

		try { _sortState = await LocalStorage.GetItemAsync<SortState>(SortKey) ?? new SortState(); }
		catch { _sortState = new SortState(); }

		await LoadCreditCards(true);
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}

	private async Task LoadCreditCards(bool useCash)
	{
		_creditCards = [.. await CashService.GetData<CreditCardInfo>(useCash)];
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}
}
