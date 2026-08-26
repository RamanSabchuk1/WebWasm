using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.JSInterop;
using WebWasm.Models;
using WebWasm.Pages;
using WebWasm.Services;

namespace WebWasm.Components;

/// <summary>
/// DevicesTable — grouped rework (Feature 3): группы по пользователю (Anonymous вместе),
/// подсветка дублей по Device (case-insensitive), чекбоксы + multi-unbind.
/// QuickGrid заменён на manual-разметку: группировку QuickGrid не поддерживает.
/// </summary>
public partial class DevicesTable : ComponentBase
{
	private const string SearchKey = "search_devices";

	[Parameter, EditorRequired] public IEnumerable<DeviceTokenWithUser> Devices { get; set; } = [];
	[Parameter] public EventCallback<(string, Guid)> OnUnbind { get; set; }
	[Parameter] public EventCallback<List<(string Token, Guid UserInfoId)>> OnUnbindMultiple { get; set; }
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private ILocalStorageService LocalStorage { get; set; } = default!;

	private readonly HashSet<Guid> _expandedTokens = [];
	private readonly HashSet<Guid> _expandedData = [];
	private readonly HashSet<string> _expandedGroups = [];
	private readonly HashSet<string> _selectedTokens = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;

	// Confirmation dialog state (single и multi unbind делят один диалог)
	private bool _showConfirmDialog = false;
	private string _confirmMessage = string.Empty;
	private List<(string Token, Guid UserInfoId)> _pendingUnbinds = [];

	private record DeviceGroup(string Key, string Name, List<DeviceTokenWithUser> Items);

	private bool IsExpanded(Guid id) => _expandedTokens.Contains(id);
	private bool IsDataExpanded(Guid id) => _expandedData.Contains(id);
	private bool IsGroupExpanded(string key) => _expandedGroups.Contains(key);
	private bool IsSelected(string token) => _selectedTokens.Contains(token);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedTokens.Remove(id))
		{
			_expandedTokens.Add(id);
		}
	}

	private void ToggleDataExpand(Guid id)
	{
		if (!_expandedData.Remove(id))
		{
			_expandedData.Add(id);
		}
	}

	private void ToggleGroupExpand(string key)
	{
		if (!_expandedGroups.Remove(key))
		{
			_expandedGroups.Add(key);
		}
	}

	private void ToggleSelect(string token)
	{
		if (!_selectedTokens.Remove(token))
		{
			_selectedTokens.Add(token);
		}
	}

	private void ClearSelection() => _selectedTokens.Clear();

	private bool IsGroupFullySelected(DeviceGroup group) =>
		group.Items.Count > 0 && group.Items.All(i => _selectedTokens.Contains(i.DeviceToken.Token));

	private void ToggleSelectGroup(DeviceGroup group)
	{
		if (IsGroupFullySelected(group))
		{
			foreach (var item in group.Items)
			{
				_selectedTokens.Remove(item.DeviceToken.Token);
			}
		}
		else
		{
			foreach (var item in group.Items)
			{
				_selectedTokens.Add(item.DeviceToken.Token);
			}
		}
	}

	private List<DeviceGroup> FilteredGroups
	{
		get
		{
			var items = Devices.AsEnumerable();

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				var lowerSearch = _searchText.ToLowerInvariant();
				items = items.Where(d =>
					d.DeviceToken.Device.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
					d.GetUserName().Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
					d.DeviceToken.Token.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
					(d.DeviceToken.AdditionalData != null && d.DeviceToken.AdditionalData.Any(kvp =>
						kvp.Key.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase) ||
						kvp.Value.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase))));
			}

			// Группировка по UserInfo.Id (null = Anonymous, одна группа). Внутри группы — по Device.
			return [.. items
				.GroupBy(d => d.User?.UserInfo.Id)
				.Select(g => new DeviceGroup(
					g.Key?.ToString() ?? "anonymous",
					g.First().GetUserName(),
					[.. g.OrderBy(d => d.DeviceToken.Device, StringComparer.OrdinalIgnoreCase)]))
				.OrderBy(g => g.Key == "anonymous" ? 1 : 0)
				.ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)];
		}
	}

	// Пагинация по группам (пользователей много — owner, 2026-08-27), механика как на Users: Skip/Take + CustomPaginator.
	private List<DeviceGroup> PagedGroups => [.. FilteredGroups
		.Skip(_pagination.CurrentPageIndex * _pagination.ItemsPerPage)
		.Take(_pagination.ItemsPerPage)];

	// Дубли: одинаковое Device (case-insensitive, trimmed) более чем у одного токена внутри группы.
	private static HashSet<string> GetDuplicateDevices(IEnumerable<DeviceTokenWithUser> items) =>
		items
			.GroupBy(d => d.DeviceToken.Device.Trim(), StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

	protected override void OnParametersSet()
	{
		// После reload часть токенов могла исчезнуть — вычищаем выделение по живым токенам.
		var alive = Devices.Select(d => d.DeviceToken.Token).ToHashSet();
		_selectedTokens.RemoveWhere(t => !alive.Contains(t));
	}

	protected override async Task OnInitializedAsync()
	{
		try { _searchText = await LocalStorage.GetItemAsync<string>(SearchKey) ?? string.Empty; }
		catch { _searchText = string.Empty; }
	}

	private async Task SaveSearch()
	{
		_ = _pagination.SetCurrentPageIndexAsync(0);
		try { await LocalStorage.SetItemAsync(SearchKey, _searchText ?? string.Empty); } catch { }
	}

	private void ShowUnbindConfirmation(string token, Guid userId, string deviceName)
	{
		_pendingUnbinds = [(token, userId)];
		_confirmMessage = $"Are you sure you want to unbind the device '{deviceName}'? This action cannot be undone.";
		_showConfirmDialog = true;
	}

	private void ShowMultiUnbindConfirmation()
	{
		if (_selectedTokens.Count == 0)
		{
			ToastService.ShowWarning("No devices selected");
			return;
		}

		_pendingUnbinds = [.. Devices
			.Where(d => _selectedTokens.Contains(d.DeviceToken.Token))
			.Select(d => (d.DeviceToken.Token, d.DeviceToken.UserInfoId))];
		_confirmMessage = $"Are you sure you want to unbind {_pendingUnbinds.Count} selected device(s)? This action cannot be undone.";
		_showConfirmDialog = true;
	}

	private async Task ConfirmUnbind()
	{
		_showConfirmDialog = false;
		var pending = _pendingUnbinds;
		_pendingUnbinds = [];
		_selectedTokens.Clear();

		if (pending.Count == 1)
		{
			await OnUnbind.InvokeAsync(pending[0]);
		}
		else
		{
			await OnUnbindMultiple.InvokeAsync(pending);
		}
	}

	private void CancelUnbind()
	{
		_showConfirmDialog = false;
		_pendingUnbinds = [];
	}

	private async Task CopyToClipboard(string text)
	{
		try
		{
			await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
			ToastService.ShowSuccess("Copied to clipboard!");
		}
		catch
		{
			// Copy failed, user will see in logs
		}
	}
}
