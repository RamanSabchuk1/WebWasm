using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class EncryptionKeys
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<EncryptionKeyInfo> _keys = [];
	private bool _showRotateDialog;
	private EncryptionPurpose _rotateTargetPurpose;
	private bool _backfillRunning;
	private BackfillResult? _lastBackfillResult;

	protected override async Task OnInitializedAsync()
	{
		await LoadKeys();
	}

	private async Task LoadKeys()
	{
		await LoadingService.Run(ToastService, async () =>
		{
			var keys = await ApiClient.Get<EncryptionKeyInfo[]>("Admin/secure-data/encryption-keys");
			_keys = [.. keys.OrderBy(k => k.Purpose).ThenByDescending(k => k.Version)];
		}, T.Keys.LoadFailed);
	}

	private bool HasActiveKey(EncryptionPurpose purpose) => _keys.Any(k => k.Purpose == purpose && k.IsActive);

	private string ActiveKeyLabel(EncryptionPurpose purpose) =>
		_keys.FirstOrDefault(k => k.Purpose == purpose && k.IsActive) is { } key ? T.Keys.ActiveVersion(key.Version) : T.Keys.NoActiveKey;

	private async Task CreateKey(EncryptionPurpose purpose)
	{
		var name = T.Enums.EncryptionPurpose(purpose);
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Admin/secure-data/encryption-key?purpose={purpose}");
			ToastService.ShowSuccess(T.Keys.Created(name));
			await LoadKeys();
		}, reason => T.Keys.CreateFailed(name, reason));
	}

	private void OpenRotateDialog(EncryptionPurpose purpose)
	{
		_rotateTargetPurpose = purpose;
		_showRotateDialog = true;
	}

	private void CloseRotateDialog()
	{
		_showRotateDialog = false;
	}

	private async Task ConfirmRotate(bool force)
	{
		var purpose = _rotateTargetPurpose;
		var name = T.Enums.EncryptionPurpose(purpose);
		_showRotateDialog = false;

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Admin/secure-data/encryption-key/rotate?purpose={purpose}&force={(force ? "true" : "false")}");
			ToastService.ShowSuccess(T.Keys.Rotated(name));
			await LoadKeys();
		}, reason => T.Keys.RotateFailed(name, reason));
	}

	private async Task RunBackfill()
	{
		_backfillRunning = true;

		// Run catches the failure itself, so the flag is always reset after it.
		await LoadingService.Run(ToastService, async () =>
		{
			var result = await ApiClient.Post<BackfillResult>("Admin/secure-data/backfill");
			_lastBackfillResult = result;
			ToastService.ShowSuccess(T.Keys.BackfillDone(result.UsersProcessed, result.CompaniesProcessed, result.CompanyRootProcessed, result.VehiclesProcessed));
		}, T.Keys.BackfillFailed);

		_backfillRunning = false;
	}
}
