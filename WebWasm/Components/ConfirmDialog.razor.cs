using Microsoft.AspNetCore.Components;

namespace WebWasm.Components;

public partial class ConfirmDialog
{
	[Parameter] public bool IsOpen { get; set; }

	/// <summary>A question: «Удалить материал?». Default: «Удалить?».</summary>
	[Parameter] public string? Title { get; set; }

	/// <summary>One line about the consequence.</summary>
	[Parameter] public string Message { get; set; } = string.Empty;

	/// <summary>The action verb. Default: «Удалить».</summary>
	[Parameter] public string? ConfirmText { get; set; }

	[Parameter] public string? CancelText { get; set; }
	[Parameter] public EventCallback OnConfirm { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }

	/// <summary>Red confirm button; false for actions that do not destroy anything.</summary>
	[Parameter] public bool IsDangerous { get; set; } = true;
}
