using Microsoft.AspNetCore.Components;

namespace WebWasm.Components;

public partial class ConfirmDialog : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public string Title { get; set; } = "Confirm";
	[Parameter] public string Message { get; set; } = string.Empty;
	[Parameter] public string ConfirmText { get; set; } = "Confirm";
	[Parameter] public string CancelText { get; set; } = "Cancel";
	[Parameter] public EventCallback OnConfirm { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }
	[Parameter] public bool IsDangerous { get; set; } = false;
}
