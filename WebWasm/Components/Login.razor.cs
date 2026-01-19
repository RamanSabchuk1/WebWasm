using Microsoft.AspNetCore.Components;

namespace WebWasm.Components;

public partial class Login(NavigationManager navManager)
{
	public record LoginModel()
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public record TokenResponse(string Token);

	private LoginModel _loginModel = new();
	private bool _isLoading = false;
	private string? _errorMessage;

	private async Task HandleLogin()
	{
		_errorMessage = null;
		_isLoading = true;

		try
		{
			if (string.IsNullOrWhiteSpace(_loginModel.Username) || string.IsNullOrWhiteSpace(_loginModel.Password))
			{
				_errorMessage = "Username and password cannot be empty.";
				return;
			}

			await ApiClient.Login(_loginModel);
			navManager.NavigateTo("/");

		}
		catch (Exception ex)
		{
			_errorMessage = $"Login failed: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
		}
	}
}
