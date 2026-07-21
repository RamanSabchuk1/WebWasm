using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class RotateKeyDialog : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public EncryptionPurpose Purpose { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<bool> OnConfirm { get; set; }

	private bool _force;

	protected override void OnParametersSet()
	{
		if (!IsOpen)
		{
			_force = false;
		}
	}

	private void OnForceChanged(ChangeEventArgs e)
	{
		_force = e.Value is bool value && value;
	}

	private async Task HandleConfirm()
	{
		await OnConfirm.InvokeAsync(_force);
	}

	private async Task CloseModal()
	{
		await OnClose.InvokeAsync();
	}
}
