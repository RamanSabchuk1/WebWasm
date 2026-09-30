using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Pages;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class SuggestionsTable : ComponentBase
{
	private const string SearchKey = "search_supports";
	private const string SortKey = "sort_suggestions";
	[Parameter, EditorRequired] public IEnumerable<Supports.SuggestionsWithUser> Suggestions { get; set; } = [];
	[Parameter] public EventCallback<Guid> OnApply { get; set; }
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private readonly HashSet<Guid> _expandedRows = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private SortState _sortState = new();

	private static readonly IReadOnlyDictionary<string, Func<Supports.SuggestionsWithUser, object?>> _sortSelectors =
		new Dictionary<string, Func<Supports.SuggestionsWithUser, object?>>
		{
			["name"] = s => s.Suggestion.Name,
			["user"] = s => s.GetUserName(),
			["created"] = s => s.Suggestion.Created,
		};

	private bool IsExpanded(Guid id) => _expandedRows.Contains(id);

	private void ToggleExpand(Guid id) => _expandedRows.Toggle(id);

	private Supports.SuggestionsWithUser[] FilteredSuggestions
	{
		get
		{
			IEnumerable<Supports.SuggestionsWithUser> items = Suggestions;

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				var lowerSearch = _searchText.ToLowerInvariant();
				items = items.Where(s =>
					s.Suggestion.Name.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
					s.Suggestion.Data.Any(kvp =>
						kvp.Key.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
						kvp.Value.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)));
			}

			return [.. SortHelper.Apply(items, _sortState, _sortSelectors)];
		}
	}

	protected override async Task OnInitializedAsync()
	{
		_searchText = await LocalStorage.GetItemOrDefaultAsync(SearchKey, string.Empty);

		_sortState = await LocalStorage.GetItemOrDefaultAsync(SortKey, new SortState());
	}

	private async Task SaveSearch()
	{
		await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty);
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		await LocalStorage.SetItemAsync(SortKey, _sortState);
	}
}
