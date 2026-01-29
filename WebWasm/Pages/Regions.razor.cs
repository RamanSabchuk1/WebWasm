using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Regions : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<Region> _regions = [];
	private RegionsTable? _regionsTableRef;
	private bool _isRegionModalOpen = false;
	private Region? _editingRegion = null;
	private bool _isDetailsModalOpen = false;
	private Region? _viewingRegion = null;
	private bool _isLevelEditorOpen = false;
	private Region? _editingLevelRegion = null;
	private Level? _editingLevel = null;
	private bool _isConfirmOpen = false;
	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private string _confirmText = string.Empty;
	private Func<Task>? _confirmAction = null;

	protected override async Task OnInitializedAsync()
	{
		await LoadRegions(true);
	}

	private async Task LoadRegions(bool useCash)
	{
		_regions = [.. await CashService.GetData<Region>(useCash)];
	}

	private void OpenAddRegionModal()
	{
		_editingRegion = null;
		_isRegionModalOpen = true;
	}

	private void HandleEditRegion(Region region)
	{
		_editingRegion = region;
		_isRegionModalOpen = true;
	}

	private void HandleViewDetails(Region region)
	{
		_viewingRegion = region;
		_isDetailsModalOpen = true;
	}

	private void CloseRegionModal()
	{
		_isRegionModalOpen = false;
		_editingRegion = null;
	}

	private void CloseDetailsModal()
	{
		_isDetailsModalOpen = false;
		_viewingRegion = null;
	}

	private void CloseLevelEditor()
	{
		_isLevelEditorOpen = false;
		_editingLevelRegion = null;
		_editingLevel = null;
	}

	private async Task HandleRegionSubmit(UpdateRegion regionData)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				if (_editingRegion is not null)
				{
					await ApiClient.Post($"Regions/{_editingRegion.Id}", regionData);
					ToastService.ShowSuccess("Region updated successfully!");
				}
				else
				{
					var createRegion = new CreateRegion(regionData.Name);
					await ApiClient.Post("Regions", createRegion);
					ToastService.ShowSuccess("Region created successfully!");
				}

				await LoadRegions(false);
				CloseRegionModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to save region: {ex.Message}");
			}
		});
	}

	private void HandleAddLevel(Region region)
	{
		_editingLevelRegion = region;
		_editingLevel = null;
		_isDetailsModalOpen = false;
		_isLevelEditorOpen = true;
	}

	private void HandleEditLevel((Region region, Level level) data)
	{
		_editingLevelRegion = data.region;
		_editingLevel = data.level;
		_isDetailsModalOpen = false;
		_isLevelEditorOpen = true;
	}

	private async Task HandleLevelSubmit(CreateLevel levelData)
	{
		if (_editingLevelRegion is null) return;

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				if (_editingLevel is not null)
				{
					// Update existing level - first delete, then create (v1 approach)
					await ApiClient.Delete($"Regions/{_editingLevelRegion.Id}/levels/{_editingLevel.Id}");
					await ApiClient.Post($"Regions/{_editingLevelRegion.Id}/levels", levelData);
					ToastService.ShowSuccess("Level updated successfully!");
				}
				else
				{
					// Create new level
					await ApiClient.Post($"Regions/{_editingLevelRegion.Id}/levels", levelData);
					ToastService.ShowSuccess("Level created successfully!");
				}

				await LoadRegions(false);
				CloseLevelEditor();
				
				// Refresh details modal if still open
				if (_isDetailsModalOpen)
				{
					_viewingRegion = _regions.FirstOrDefault(r => r.Id == _editingLevelRegion.Id);
				}
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to save level: {ex.Message}");
			}
		});
	}

	private void HandleDeleteRegion(Region region)
	{
		_confirmTitle = "Delete Region";
		_confirmMessage = $"Are you sure you want to delete the region \"{region.Name}\"? This will also delete all its levels and associated data.";
		_confirmText = "Delete";
		_confirmAction = async () => await DeleteRegion(region.Id);
		_isConfirmOpen = true;
	}

	private async Task DeleteRegion(Guid regionId)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete($"Regions/{regionId}");
				ToastService.ShowSuccess("Region deleted successfully!");
				await LoadRegions(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete region: {ex.Message}");
			}
		});
	}

	private void HandleDeleteLevel((Region region, Guid levelId) data)
	{
		var level = data.region.Levels?.FirstOrDefault(l => l.Id == data.levelId);
		if (level is null) return;

		_confirmTitle = "Delete Level";
		_confirmMessage = $"Are you sure you want to delete the {level.Type} level? This will remove all {level.Triangles?.Count ?? 0} triangles in this level.";
		_confirmText = "Delete";
		_confirmAction = async () => await DeleteLevel(data.region.Id, data.levelId);
		_isConfirmOpen = true;
	}

	private async Task DeleteLevel(Guid regionId, Guid levelId)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete($"Regions/{regionId}/levels/{levelId}");
				ToastService.ShowSuccess("Level deleted successfully!");
				await LoadRegions(false);
				
				// Refresh the details modal if open
				if (_isDetailsModalOpen && _viewingRegion?.Id == regionId)
				{
					_viewingRegion = _regions.FirstOrDefault(r => r.Id == regionId);
				}
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete level: {ex.Message}");
			}
		});
	}

	private async Task HandleConfirm()
	{
		if (_confirmAction is not null)
		{
			await _confirmAction();
		}
		CloseConfirm();
	}

	private void CloseConfirm()
	{
		_isConfirmOpen = false;
		_confirmAction = null;
	}
}
