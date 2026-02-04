using WebWasm.Services;

namespace WebWasm.Pages;

public partial class MyUserInfo(ApiClient apiClient, LoadingService loadingService, CashService cashService)
{
    private UserInfoModel _userInfo = new();
    private bool _isSaving = false;
    private bool _saveSuccess = false;

    protected override void OnInitialized()
    {
        // Load user data (in real app, fetch from API)
        _userInfo = new UserInfoModel
        {
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@example.com",
            Role = "SuperAdmin"
        };
    }

    private async Task HandleSave()
    {
        _isSaving = true;
        _saveSuccess = false;

        // Simulate API call
        await Task.Delay(500);

        _isSaving = false;
        _saveSuccess = true;

        // Hide success message after 3 seconds
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            _saveSuccess = false;
            InvokeAsync(StateHasChanged);
        });
    }

    public class UserInfoModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
