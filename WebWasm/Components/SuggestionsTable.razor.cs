using System.Diagnostics.CodeAnalysis;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Pages;

namespace WebWasm.Components;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "AsQueryable is used for in-memory QuickGrid binding only")]
public partial class SuggestionsTable : ComponentBase
{
	private const string SearchKey = "search_supports";
	[Parameter, EditorRequired] public IEnumerable<Supports.SuggestionsWithUser> Suggestions { get; set; } = [];
	[Parameter] public EventCallback<Guid> OnApply { get; set; }
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private readonly HashSet<Guid> _expandedRows = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredSuggestions.Any();

	private bool IsExpanded(Guid id) => _expandedRows.Contains(id);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedRows.Remove(id))
			_expandedRows.Add(id);
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

			return items;
		}
	}

	protected override async Task OnInitializedAsync()
	{
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }
	}

	private async Task SaveSearch()
	{
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}
}
