using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class MyUserInfo(ApiClient apiClient, LoadingService loadingService, CashService cashService, ToastService toastService)
{
	private UserInfo? _userInfo;
	private SetUserNames _updateUser = new(string.Empty, string.Empty, string.Empty);

	protected override async Task OnInitializedAsync()
	{
		await LoadData(true);
	}

	private async Task LoadData(bool useCash)
	{
		_userInfo = await cashService.GetUserInfo(useCash);
		_updateUser = new SetUserNames(_userInfo?.FirstName ?? string.Empty, _userInfo?.MiddleName, _userInfo?.LastName ?? string.Empty);
		StateHasChanged();
	}

	private async Task HandleSave()
	{
		await loadingService.ExecuteWithLoading(async () => {
			try
			{
				await apiClient.Put("Users/user-info", _updateUser);
				await LoadData(false);
				toastService.ShowSuccess("Company created successfully!");
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to create company: {ex.Message}");
			}
		});
	}
}
