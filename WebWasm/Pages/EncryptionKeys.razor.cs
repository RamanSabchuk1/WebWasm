using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class EncryptionKeys : ComponentBase
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
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var keys = await ApiClient.Get<EncryptionKeyInfo[]>("Admin/secure-data/encryption-keys");
				_keys = [.. keys.OrderBy(k => k.Purpose).ThenByDescending(k => k.Version)];
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to load encryption keys: {ex.Message}");
			}
		});
	}

	private bool HasActiveKey(EncryptionPurpose purpose) => _keys.Any(k => k.Purpose == purpose && k.IsActive);

	private async Task CreateKey(EncryptionPurpose purpose)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Admin/secure-data/encryption-key?purpose={purpose}");
				ToastService.ShowSuccess($"{purpose} encryption key created successfully.");
				await LoadKeys();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create {purpose} key: {ex.Message}");
			}
		});
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
		_showRotateDialog = false;

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Admin/secure-data/encryption-key/rotate?purpose={purpose}&force={(force ? "true" : "false")}");
				ToastService.ShowSuccess($"{purpose} encryption key rotated successfully. Existing data will be re-encrypted gradually in the background.");
				await LoadKeys();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to rotate {purpose} key: {ex.Message}");
			}
		});
	}

	private async Task RunBackfill()
	{
		_backfillRunning = true;

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				_lastBackfillResult = await ApiClient.Post<BackfillResult>("Admin/secure-data/backfill");
				ToastService.ShowSuccess(
					$"Backfill complete: {_lastBackfillResult.UsersProcessed} users, " +
					$"{_lastBackfillResult.CompaniesProcessed} companies (info), " +
					$"{_lastBackfillResult.CompanyRootProcessed} companies (root), " +
					$"{_lastBackfillResult.VehiclesProcessed} vehicles.");
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Backfill failed: {ex.Message}");
			}
			finally
			{
				_backfillRunning = false;
			}
		});
	}
}
