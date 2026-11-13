using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Components;

/// <summary>
/// Views and changes the data security level of one entity. Loads the current level from <see cref="Endpoint"/>
/// when opened and saves it back there; the page only says whose level it is and reloads on <see cref="OnSaved"/>.
/// </summary>
public partial class SecurityLevelDialog
{
	private static readonly DataSecurityLevel[] _levels = Enum.GetValues<DataSecurityLevel>();

	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;

	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public string EntityLabel { get; set; } = string.Empty;
	/// <summary>API path of the entity's level, e.g. <c>admin/security-levels/users/{id}</c>.</summary>
	[Parameter] public string Endpoint { get; set; } = string.Empty;
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback OnSaved { get; set; }

	private DataSecurityLevel _currentLevel;
	private DataSecurityLevel _selectedLevel;
	private bool _loaded;
	private bool _wasOpen;

	protected override async Task OnParametersSetAsync()
	{
		if (IsOpen == _wasOpen)
		{
			return;
		}

		_wasOpen = IsOpen;
		_loaded = false;
		if (!IsOpen)
		{
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			var response = await ApiClient.Get<SecurityLevelRequest>(Endpoint);
			_currentLevel = _selectedLevel = response.Level;
			_loaded = true;
		}, T.Security.LoadFailed);

		if (!_loaded)
		{
			await OnClose.InvokeAsync();
		}
	}

	private async Task HandleSubmit()
	{
		if (!_loaded)
		{
			return;
		}

		var endpoint = Endpoint;
		var newLevel = _selectedLevel;
		var changed = newLevel != _currentLevel;
		await OnClose.InvokeAsync();

		if (!changed)
		{
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Put(endpoint, new SecurityLevelRequest(newLevel));
			await OnSaved.InvokeAsync();
			ToastService.ShowSuccess(T.Security.Saved);
		}, T.Security.SaveFailed);
	}

	private async Task CloseModal()
	{
		await OnClose.InvokeAsync();
	}
}
