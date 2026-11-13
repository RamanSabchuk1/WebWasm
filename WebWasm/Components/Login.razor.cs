using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class Login(NavigationManager navManager, ApiClient apiClient, AppSettings settings)
{
	public record LoginModel()
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public record TokenResponse(string Token);

	private readonly LoginModel _loginModel = new();
	private bool _isLoading = false;
	private string? _errorMessage;

	private AppSettings Settings => settings;

	private async Task HandleLogin()
	{
		_errorMessage = null;
		_isLoading = true;

		try
		{
			if (string.IsNullOrWhiteSpace(_loginModel.Username) || string.IsNullOrWhiteSpace(_loginModel.Password))
			{
				_errorMessage = T.Login.Empty;
				return;
			}

			await apiClient.Login(_loginModel);
			navManager.NavigateTo("myuserinfo");
		}
		catch (Exception ex)
		{
			_errorMessage = T.Login.Failed(ex.Message);
		}
		finally
		{
			_isLoading = false;
		}
	}
}
