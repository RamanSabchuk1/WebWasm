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
	private bool _isRegionModalOpen = false;
	private Region? _editingRegion = null;
	private bool _isDetailsModalOpen = false;
	private Region? _viewingRegion = null;
	private bool _isLevelEditorOpen = false;
	private Region? _editingLevelRegion = null;
	private Level? _editingLevel = null;
	private readonly ConfirmState _confirm = new();

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
		await LoadingService.Run(ToastService, async () =>
		{
			if (_editingRegion is not null)
			{
				await ApiClient.Put($"Regions/{_editingRegion.Id}", regionData);
				ToastService.ShowSuccess("Region updated successfully!");
			}
			else
			{
				var createRegion = new CreateRegion(regionData.Name, regionData.TimeZone);
				await ApiClient.Post("Regions", createRegion);
				ToastService.ShowSuccess("Region created successfully!");
			}

			await LoadRegions(false);
			CloseRegionModal();
		}, "Failed to save region: ");
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

	private async Task HandleLevelSubmit(MutateLevel levelData)
	{
		if (_editingLevelRegion is null)
		{
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			// Create-only path: edit goes through the split handlers below
			await ApiClient.Post($"Regions/{_editingLevelRegion.Id}/level", levelData);
			ToastService.ShowSuccess("Level created successfully!");

			await LoadRegions(false);
			CloseLevelEditor();

			if (_isDetailsModalOpen)
			{
				_viewingRegion = _regions.FirstOrDefault(r => r.Id == _editingLevelRegion.Id);
			}
		}, "Failed to save level: ");
	}

	private async Task HandleLevelSubmitPrices(UpdateLevelPriceInfo priceInfo)
	{
		if (_editingLevelRegion is null || _editingLevel is null)
		{
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Patch($"Regions/{_editingLevelRegion.Id}/level/{_editingLevel.Id}/price-info", priceInfo);
			ToastService.ShowSuccess("Prices updated successfully!");

			await LoadRegions(false);

			if (_isDetailsModalOpen)
			{
				_viewingRegion = _regions.FirstOrDefault(r => r.Id == _editingLevelRegion.Id);
			}
		}, "Failed to update prices: ");
	}

	private async Task HandleLevelSubmitGeometry(UpdateLevelGeometry geometry)
	{
		if (_editingLevelRegion is null || _editingLevel is null)
		{
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Patch($"Regions/{_editingLevelRegion.Id}/level/{_editingLevel.Id}/geometry", geometry);
			ToastService.ShowSuccess("Geometry updated successfully!");

			await LoadRegions(false);

			if (_isDetailsModalOpen)
			{
				_viewingRegion = _regions.FirstOrDefault(r => r.Id == _editingLevelRegion.Id);
			}
		}, "Failed to update geometry: ");
	}

	private void HandleDeleteRegion(Region region)
	{
		_confirm.Ask("Delete Region", $"Are you sure you want to delete the region \"{region.Name}\"? This will also delete all its levels and associated data.", async () => await DeleteRegion(region.Id));
	}

	private async Task DeleteRegion(Guid regionId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Regions/{regionId}");
			ToastService.ShowSuccess("Region deleted successfully!");
			await LoadRegions(false);
		}, "Failed to delete region: ");
	}

	private void HandleDeleteLevel((Region region, Guid levelId) data)
	{
		var level = data.region.Levels?.FirstOrDefault(l => l.Id == data.levelId);
		if (level is null)
		{
			return;
		}

		_confirm.Ask("Delete Level", $"Are you sure you want to delete the {level.Type} level? This will remove all {level.Triangles?.Count ?? 0} triangles in this level.", async () => await DeleteLevel(data.region.Id, data.levelId));
	}

	private async Task DeleteLevel(Guid regionId, Guid levelId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Regions/{regionId}/level/{levelId}");
			ToastService.ShowSuccess("Level deleted successfully!");
			await LoadRegions(false);

			// Refresh the details modal if open
			if (_isDetailsModalOpen && _viewingRegion?.Id == regionId)
			{
				_viewingRegion = _regions.FirstOrDefault(r => r.Id == regionId);
			}
		}, "Failed to delete level: ");
	}
}
