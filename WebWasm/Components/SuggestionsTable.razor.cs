using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using System.Diagnostics.CodeAnalysis;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Pages;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class SuggestionsTable : ComponentBase
{
	private const string SearchKey = "search_supports";
	private const string SortKey = "sort_suggestions";
	[Parameter, EditorRequired] public IEnumerable<Supports.SuggestionsWithUser> Suggestions { get; set; } = [];
	[Parameter] public EventCallback<Guid> OnApply { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private readonly HashSet<Guid> _expandedRows = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private SortState _sortState = new();
	private bool _hasItems => FilteredSuggestions.Any();

	private static readonly IReadOnlyDictionary<string, Func<Supports.SuggestionsWithUser, object?>> _sortSelectors =
		new Dictionary<string, Func<Supports.SuggestionsWithUser, object?>>
		{
			["name"] = s => s.Suggestion.Name,
			["user"] = s => s.GetUserName(),
			["created"] = s => s.Suggestion.Created,
		};

	private bool IsExpanded(Guid id) => _expandedRows.Contains(id);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedRows.Remove(id))
		{
			_expandedRows.Add(id);
		}
	}

	private IQueryable<Supports.SuggestionsWithUser> FilteredSuggestions
	{
		get
		{
			var items = Suggestions.AsQueryable();

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				var lowerSearch = _searchText.ToLowerInvariant();
				items = items.Where(s =>
					s.Suggestion.Name.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
					s.Suggestion.Data.Any(kvp =>
						kvp.Key.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
						kvp.Value.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)));
			}

			return SortHelper.Apply(items, _sortState, _sortSelectors).AsQueryable();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }

		try { _sortState = await LocalStorage.GetItemAsync<SortState>(SortKey) ?? new SortState(); }
		catch { _sortState = new SortState(); }
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}

	private async Task CycleSort(string columnKey)
	{
		_sortState = SortHelper.Cycle(_sortState, columnKey);
		try { await LocalStorage.SetItemAsync(SortKey, _sortState); } catch { }
	}
}
