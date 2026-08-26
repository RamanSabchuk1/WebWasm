using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Devices : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<DeviceTokenWithUser> _devicesWithUsers = [];
	private DevicesTable? _devicesTableRef;

	protected override async Task OnInitializedAsync()
	{
		await LoadDevices(true);
	}

	private async Task LoadDevices(bool useCash)
	{
		var deviceTokens = await CashService.GetData<DeviceToken>(useCash);
		var users = await CashService.GetData<User>(useCash);

		var userDict = users.ToDictionary(u => u.UserInfo.Id);

		_devicesWithUsers = [.. deviceTokens
			.Select(dt => new DeviceTokenWithUser(
				dt,
				userDict.TryGetValue(dt.UserInfoId, out var user) ? user : null
			))];
	}

	private async Task HandleUnbind((string, Guid) @event)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var (deviceTokenId, userInfoId) = @event;
				await ApiClient.Delete($"DeviceTokens/{deviceTokenId}?userInfoId={userInfoId}");
				ToastService.ShowSuccess("Device unbound successfully!");
				await LoadDevices(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to unbind device: {ex.Message}");
			}
		});
	}

	// Batch-endpoint'а нет (owner, 2026-08-27): N последовательных single DELETE — приемлемо для admin-объёмов.
	private async Task HandleMultiUnbind(List<(string Token, Guid UserInfoId)> tokens)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			var failed = 0;
			foreach (var (token, userInfoId) in tokens)
			{
				try
				{
					await ApiClient.Delete($"DeviceTokens/{token}?userInfoId={userInfoId}");
				}
				catch
				{
					failed++;
				}
			}

			if (failed == 0)
			{
				ToastService.ShowSuccess($"Unbound {tokens.Count} device(s) successfully!");
			}
			else
			{
				ToastService.ShowError($"Unbound {tokens.Count - failed} of {tokens.Count} device(s); {failed} failed.");
			}

			await LoadDevices(false);
		});
	}
}

public record DeviceTokenWithUser(DeviceToken DeviceToken, User? User)
{
	public string GetUserName()
	{
		return User is null
			? "Anonymous"
			: string.IsNullOrEmpty(User.UserInfo.FirstName)
				? User.Login
				: $"{User.UserInfo.FirstName} {User.UserInfo.LastName}";
	}
}
