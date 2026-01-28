using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Pages;

namespace WebWasm.Components;

public partial class SuggestionsTable : ComponentBase
{
	[Parameter, EditorRequired] public IEnumerable<Supports.SuggestionsWithUser> Suggestions { get; set; } = [];
	[Parameter] public EventCallback<Guid> OnApply { get; set; }

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
