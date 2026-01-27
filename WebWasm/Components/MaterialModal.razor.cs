using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class MaterialModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public MaterialType? EditingMaterial { get; set; }
	[Parameter] public List<MaterialType> AllMaterials { get; set; } = [];
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<MaterialTypeInfo> OnSubmit { get; set; }

	private string _name = string.Empty;
	private string _description = string.Empty;
	private string _solidity = string.Empty;
	private string _photo = string.Empty;
	private string _parentId = string.Empty;
	private string _errorMessage = string.Empty;

	private bool IsEditMode => EditingMaterial is not null;
	private bool IsValid => !string.IsNullOrWhiteSpace(_name) 
		&& !string.IsNullOrWhiteSpace(_description) 
		&& !string.IsNullOrWhiteSpace(_solidity);

	// Simplified logic for v0/v1:
	// - New materials: can only select root materials as parent
	// - Editing with parent: can only select root materials as parent
	// - Editing root material: cannot set parent
	private bool CanSetParent => !IsEditMode || EditingMaterial?.ParentId != null;
	
	private List<MaterialType> AvailableMaterials => AllMaterials
		.Where(m => m.ParentId == null) // Only root materials can be parents
		.ToList();

	protected override void OnParametersSet()
	{
		if (IsOpen)
		{
			if (IsEditMode && EditingMaterial is not null)
			{
				_name = EditingMaterial.Name;
				_description = EditingMaterial.Description;
				_solidity = EditingMaterial.Solidity;
				_photo = EditingMaterial.Photo ?? string.Empty;
				_parentId = EditingMaterial.ParentId?.ToString() ?? string.Empty;
			}
			else
			{
				ResetForm();
			}
			_errorMessage = string.Empty;
		}
	}

	private async Task HandleSubmit()
	{
		if (!IsValid)
		{
			_errorMessage = "Please fill in all required fields.";
			return;
		}

		Guid? parentId = string.IsNullOrEmpty(_parentId) ? null : Guid.Parse(_parentId);
		var materialInfo = new MaterialTypeInfo(
			parentId,
			_name.Trim(),
			_description.Trim(),
			_solidity.Trim(),
			_photo.Trim()
		);

		await OnSubmit.InvokeAsync(materialInfo);
		ResetForm();
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}

	private void ResetForm()
	{
		_name = string.Empty;
		_description = string.Empty;
		_solidity = string.Empty;
		_photo = string.Empty;
		_parentId = string.Empty;
		_errorMessage = string.Empty;
	}
}
