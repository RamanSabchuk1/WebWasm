using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class Payments : ComponentBase
{
	private const string SearchKey = "search_payments";
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

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
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }
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
}
