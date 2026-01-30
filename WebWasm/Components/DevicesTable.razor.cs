using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.JSInterop;
using WebWasm.Pages;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class DevicesTable : ComponentBase
{
	[Parameter, EditorRequired] public IEnumerable<DeviceTokenWithUser> Devices { get; set; } = [];
	[Parameter] public EventCallback<(string, Guid)> OnUnbind { get; set; }
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;

	private readonly HashSet<Guid> _expandedTokens = [];
	private readonly HashSet<Guid> _expandedData = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private string _searchText = string.Empty;
	private bool _hasItems => FilteredDevices.Any();

	// Confirmation dialog state
	private bool _showConfirmDialog = false;
	private string _confirmMessage = string.Empty;
	private string _pendingToken = string.Empty;
	private Guid _pendingUserId = Guid.Empty;

	private bool IsExpanded(Guid id) => _expandedTokens.Contains(id);
	private bool IsDataExpanded(Guid id) => _expandedData.Contains(id);

	private void ToggleExpand(Guid id)
	{
		if (!_expandedTokens.Remove(id))
			_expandedTokens.Add(id);
	}

	private void ToggleDataExpand(Guid id)
	{
		if (!_expandedData.Remove(id))
			_expandedData.Add(id);
	}

	private void ShowUnbindConfirmation(string token, Guid userId, string deviceName)
	{
		_pendingToken = token;
		_pendingUserId = userId;
		_confirmMessage = $"Are you sure you want to unbind the device '{deviceName}'? This action cannot be undone.";
		_showConfirmDialog = true;
	}

	private async Task ConfirmUnbind()
	{
		_showConfirmDialog = false;
		await OnUnbind.InvokeAsync((_pendingToken, _pendingUserId));
		_pendingToken = string.Empty;
		_pendingUserId = Guid.Empty;
	}

	private void CancelUnbind()
	{
		_showConfirmDialog = false;
		_pendingToken = string.Empty;
		_pendingUserId = Guid.Empty;
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

	private IQueryable<DeviceTokenWithUser> FilteredDevices
	{
		get
		{
			var items = Devices.AsQueryable();

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
