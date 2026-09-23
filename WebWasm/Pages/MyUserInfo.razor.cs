using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class MyUserInfo(ApiClient apiClient, LoadingService loadingService, CashService cashService, ToastService toastService)
{
	[Inject] public IJSRuntime JSRuntime { get; set; } = default!;
	[Inject] public NavigationManager NavigationManager { get; set; } = default!;

	private UserInfo? _userInfo;
	private SetUserNames _updateUser = new(string.Empty, string.Empty, string.Empty);
	private List<PushNotificationItem> _notifications = [];

	// ФИО меняют один раз, поэтому по умолчанию показываем их текстом, а не формой.
	private bool _isEditingNames;

	private string FullName
	{
		get
		{
			var parts = new[] { _userInfo?.LastName, _userInfo?.FirstName, _userInfo?.MiddleName }
				.Where(x => !string.IsNullOrWhiteSpace(x));
			var name = string.Join(' ', parts);
			return string.IsNullOrWhiteSpace(name) ? "Имя не заполнено" : name;
		}
	}

	private string Initials
	{
		get
		{
			var first = _userInfo?.FirstName?.Trim();
			var last = _userInfo?.LastName?.Trim();
			var initials = string.Concat(
				string.IsNullOrEmpty(first) ? string.Empty : first[..1],
				string.IsNullOrEmpty(last) ? string.Empty : last[..1]);
			return string.IsNullOrEmpty(initials) ? "👤" : initials.ToUpperInvariant();
		}
	}

	private void StartEditNames()
	{
		_updateUser = new SetUserNames(_userInfo?.FirstName ?? string.Empty, _userInfo?.MiddleName, _userInfo?.LastName ?? string.Empty);
		_isEditingNames = true;
	}

	private void CancelEditNames() => _isEditingNames = false;

	/// <summary>Кликабельны только уведомления с действием — иначе курсор обещает переход, которого нет.</summary>
	private static bool IsClickable(PushNotificationItem note)
	{
		if (note.Data.ValueKind != JsonValueKind.Object)
		{
			return false;
		}

		var action = GetProperty(note.Data, "clickAction");
		return (action is "OPEN_ORDER_DETAILS" or "OPEN_DELIVERY_DETAILS")
			&& !string.IsNullOrEmpty(GetProperty(note.Data, "orderId"));
	}

	protected override async Task OnInitializedAsync()
	{
		await LoadData(true);
		await LoadNotifications();
	}

	private async Task LoadNotifications()
	{
		try
		{
			_notifications = await JSRuntime.InvokeAsync<List<PushNotificationItem>>("serviceWorkerInterop.getNotifications");
		}
		catch (Exception ex)
		{
			toastService.ShowError($"Failed to load notifications: {ex.Message}");
		}
	}

	private void NavigateToNotification(PushNotificationItem note)
	{
		try
		{
			if (note.Data.ValueKind == JsonValueKind.Object)
			{
				var action = GetProperty(note.Data, "clickAction");
				var orderId = GetProperty(note.Data, "orderId");

				if ((action == "OPEN_ORDER_DETAILS" || action == "OPEN_DELIVERY_DETAILS") && !string.IsNullOrEmpty(orderId))
				{
					NavigationManager.NavigateTo($"orders/{orderId}");
				}
			}
		}
		catch { }
	}

	private static string? GetProperty(JsonElement element, string name)
	{
		return element.TryGetProperty(name, out var prop) ? prop.GetString() : null;
	}

	private async Task LoadData(bool useCash)
	{
		_userInfo = await cashService.GetUserInfo(useCash);
		_updateUser = new SetUserNames(_userInfo?.FirstName ?? string.Empty, _userInfo?.MiddleName, _userInfo?.LastName ?? string.Empty);
	}

	private async Task HandleSave()
	{
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await apiClient.Put("Users/user-info", _updateUser);
				await LoadData(false);
				_isEditingNames = false;
				toastService.ShowSuccess("User successfully updated!");
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to Update user: {ex.Message}");
			}
		});
	}
}

public class PushNotificationItem
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string Body { get; set; } = string.Empty;
	public JsonElement Data { get; set; }
	public DateTime Timestamp { get; set; }
}
