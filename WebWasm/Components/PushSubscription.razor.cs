using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class PushSubscription : ComponentBase
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = default!;

	/// <summary>Компактный режим (колокольчик) для шапки профиля.</summary>
	[Parameter] public bool Compact { get; set; }

	private string? _fcmToken;
	private string? _errorMessage;
	private bool _isLoading = true;
	private bool _isSubscribing;

	private bool IsSubscribed => !string.IsNullOrEmpty(_fcmToken);
	private bool HasError => !string.IsNullOrEmpty(_errorMessage);

	private string BellTitle => (_isSubscribing, IsSubscribed, HasError) switch
	{
		(true, _, _) => "Subscribing…",
		(_, true, _) => "Push-уведомления включены",
		(_, _, true) => _errorMessage!,
		_ => "Включить push-уведомления",
	};

	private string BellCaption => (IsSubscribed, HasError) switch
	{
		(true, _) => "Включены",
		(_, true) => "Недоступно",
		_ => "Уведомления",
	};

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		bool isSupported;
		try
		{
			isSupported = await JSRuntime.InvokeAsync<bool>("eval", "'Notification' in window && 'serviceWorker' in navigator");
		}
		catch (JSException)
		{
			isSupported = false;
		}

		if (!isSupported)
		{
			_errorMessage = "Push notifications are not supported in this browser.";
		}

		_isLoading = false;
		StateHasChanged();
	}

	private async Task Subscribe()
	{
		_errorMessage = null;
		_isSubscribing = true;

		try
		{
			var permission = await JSRuntime.InvokeAsync<string>("Notification.requestPermission");
			if (permission != "granted")
			{
				_errorMessage = "Notification permission was denied. Please enable notifications in your browser settings.";
				return;
			}

			var token = await JSRuntime.InvokeAsync<string?>("getFcmToken");
			if (string.IsNullOrEmpty(token))
			{
				_errorMessage = "Could not retrieve FCM token. Please check the browser console for more details.";
				return;
			}

			_fcmToken = token;
			var info = new DeviceTokenInfo(token, "WebWasm PWA", new Dictionary<string, string>
			{
				{ "platform", "web" },
				{ "userAgent", await JSRuntime.InvokeAsync<string>("eval", "navigator.userAgent") }
			});

			await ApiClient.Post("DeviceTokens", info);
		}
		catch (Exception ex)
		{
			_errorMessage = $"An error occurred: {ex.Message}";
		}
		finally
		{
			_isSubscribing = false;
		}
	}
}
