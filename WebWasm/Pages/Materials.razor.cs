using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Materials : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<MaterialType> _materials = [];
	private bool _isModalOpen = false;
	private MaterialType? _editingMaterial = null;
	private MaterialsTable? _materialsTableRef;

	protected override async Task OnInitializedAsync()
	{
		await LoadMaterials(true);
	}

	private async Task LoadMaterials(bool useCash)
	{
		_materials = [.. await CashService.GetData<MaterialType>(useCash, async () => await GoToPage(0))];
	}

	private void OpenAddModal()
	{
		_editingMaterial = null;
		_isModalOpen = true;
	}

	private void HandleEdit(MaterialType material)
	{
		_editingMaterial = material;
		_isModalOpen = true;
	}

	private void CloseModal()
	{
		_isModalOpen = false;
		_editingMaterial = null;
	}

	private async Task HandleSubmit(MaterialTypeInfo materialInfo)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				if (_editingMaterial is not null)
				{
					// Update existing material - using POST with ID in route
					await ApiClient.Post($"MaterialTypes/{_editingMaterial.Id}", materialInfo);
					ToastService.ShowSuccess("Material updated successfully!");
				}
				else
				{
					// Create new material
					await ApiClient.Post("MaterialTypes", materialInfo);
					ToastService.ShowSuccess("Material created successfully!");
				}

				await LoadMaterials(false);
				CloseModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to save material: {ex.Message}");
			}
		});
	}

	private async Task HandleDelete(Guid materialId)
	{
		// Check if material has children
		var hasChildren = _materials.Any(m => m.ParentId == materialId);
		if (hasChildren)
		{
			ToastService.ShowError("Cannot delete material with children. Please delete or reassign child materials first.");
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Delete($"MaterialTypes/{materialId}");
				ToastService.ShowSuccess("Material deleted successfully!");
				await LoadMaterials(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to delete material: {ex.Message}");
			}
		});
	}

	/// <summary>
	/// Example method to demonstrate programmatic page navigation
	/// Can be called from UI or other methods
	/// </summary>
	public async Task GoToPage(int pageNumber)
	{
		if (_materialsTableRef is not null)
		{
			await _materialsTableRef.SetPage(pageNumber);
		}
	}
}
