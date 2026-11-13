using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class PushSubscription(ApiClient apiClient, AppSettings settings, IJSRuntime js) : IAsyncDisposable
{
	private ElementReference _button;
	private IJSObjectReference? _module;
	private DotNetObjectReference<PushSubscription>? _self;
	private PushState _state = PushState.Checking;
	private string? _error;

	private bool CanTap => _state is PushState.Ready or PushState.On or PushState.Failed;

	private string Title => _state switch
	{
		PushState.On => T.Push.Rebind,
		PushState.Failed => _error ?? T.Push.FailedTitle,
		_ => T.Push.Enable,
	};

	private string Caption => _state switch
	{
		PushState.Checking => T.Push.Checking,
		PushState.Subscribing => T.Push.Subscribing,
		PushState.On => T.Push.On,
		PushState.Ready => T.Push.Notifications,
		PushState.Denied => T.Push.Denied,
		PushState.Failed => T.Push.Error,
		_ => T.Push.Unavailable,
	};

	private string? Hint => _state switch
	{
		PushState.InstallRequired => T.Push.InstallRequired,
		PushState.Denied => T.Push.DeniedHint,
		PushState.Unsupported => T.Push.UnsupportedHint,
		_ => null,
	};

	private string DeviceName => settings.EnvironmentLabel is null ? $"{settings.Name} PWA" : $"{settings.Name} {settings.EnvironmentLabel} PWA";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		try
		{
			_module = await js.InvokeAsync<IJSObjectReference>("import", "./Components/PushSubscription.razor.js");
			_self = DotNetObjectReference.Create(this);
			_state = ParseStatus(await _module.InvokeAsync<string>("attach", _button, _self));
		}
		catch (JSException ex)
		{
			Console.WriteLine($"Push subscription is unavailable: {ex.Message}");
			_state = PushState.Unsupported;
		}

		StateHasChanged();
	}

	[JSInvokable]
	public void OnSubscribing()
	{
		_state = PushState.Subscribing;
		_error = null;
		StateHasChanged();
	}

	[JSInvokable]
	public async Task OnSubscribeResult(string status, string? token, string? error, string userAgent)
	{
		if (status == "granted" && !string.IsNullOrEmpty(token))
		{
			try
			{
				await apiClient.Post("DeviceTokens", new DeviceTokenInfo(token, DeviceName, new Dictionary<string, string>
				{
					["platform"] = "web",
					["userAgent"] = userAgent,
				}));
				// «Включены» — только когда backend принял токен.
				_state = PushState.On;
			}
			catch (Exception ex)
			{
				_state = PushState.Failed;
				_error = ex.Message;
			}
		}
		else
		{
			_state = status == "granted" ? PushState.Failed : ParseStatus(status);
			_error = error ?? (status == "granted" ? T.Push.NoToken : null);
		}

		StateHasChanged();
	}

	private static PushState ParseStatus(string status) => status switch
	{
		"subscribed" => PushState.On,
		"default" or "granted" => PushState.Ready,
		"denied" => PushState.Denied,
		"install-required" => PushState.InstallRequired,
		"error" => PushState.Failed,
		_ => PushState.Unsupported,
	};

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (_module is not null)
		{
			try
			{
				await _module.InvokeVoidAsync("detach", _button);
				await _module.DisposeAsync();
			}
			catch (JSDisconnectedException)
			{
			}
		}

		_self?.Dispose();
	}

	private enum PushState
	{
		Checking,
		Ready,
		Subscribing,
		On,
		Denied,
		InstallRequired,
		Unsupported,
		Failed,
	}
}
