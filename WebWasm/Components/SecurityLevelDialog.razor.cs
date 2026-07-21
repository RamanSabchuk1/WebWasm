using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class SecurityLevelDialog : ComponentBase
{
	private static readonly DataSecurityLevel[] _levels = Enum.GetValues<DataSecurityLevel>();

	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public string EntityLabel { get; set; } = string.Empty;
	[Parameter] public DataSecurityLevel CurrentLevel { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<DataSecurityLevel> OnSubmit { get; set; }

	private DataSecurityLevel _selectedLevel;
	private string _errorMessage = string.Empty;
	private bool _selectionInitialized;

	protected override void OnParametersSet()
	{
		if (!IsOpen)
		{
			_selectionInitialized = false;
			_errorMessage = string.Empty;
			return;
		}

		if (!_selectionInitialized)
		{
			_selectedLevel = CurrentLevel;
			_selectionInitialized = true;
		}
	}

	private async Task HandleSubmit()
	{
		await OnSubmit.InvokeAsync(_selectedLevel);
	}

	private async Task CloseModal()
	{
		await OnClose.InvokeAsync();
	}

	private static string GetDescription(DataSecurityLevel level) => level switch
	{
		DataSecurityLevel.Public => "Public data — no restrictions.",
		DataSecurityLevel.Internal => "Internal company data (e.g. corporate email).",
		DataSecurityLevel.CompanyOperational => "Company operational data (e.g. mobile phone, registration number).",
		DataSecurityLevel.Restricted => "Restricted access (e.g. address, UNP, BIC).",
		DataSecurityLevel.Sensitive => "Sensitive data — passport fields, bank number.",
		DataSecurityLevel.SystemOnly => "System-only access — reserved.",
		_ => string.Empty
	};
}
