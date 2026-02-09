using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
		await loadingService.ExecuteWithLoading(async () => {
			try
			{
				await apiClient.Put("Users/user-info", _updateUser);
				await LoadData(false);
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
