using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class CompanyModal : ComponentBase
{
	[Parameter] public Company? EditingCompany { get; set; }
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<CompanySubmit> OnSubmit { get; set; }
	private bool IsEditMode => EditingCompany is not null;

	private string _name = string.Empty;
	private string _address = string.Empty;
	private string _corporateEmail = string.Empty;
	private string _unp = string.Empty;
	private string _legalType = string.Empty;
	private string _bankNumber = string.Empty;
	private string _bic = string.Empty;
	private string _photo = string.Empty;
	private double _rebate;
	private Location? _location;
	private string _errorMessage = string.Empty;

	private bool _wasOpen;

	protected override void OnParametersSet()
	{
		// Форма заполняется только при открытии: перерисовка родителя (например после ошибки сохранения) не стирает ввод.
		if (IsOpen && !_wasOpen)
		{
			ResetForm();
			if (EditingCompany is not null)
			{
				var info = EditingCompany.CompanyInfo;
				_name = EditingCompany.Name;
				_address = info?.Address ?? string.Empty;
				_corporateEmail = info?.CorporateEmail ?? string.Empty;
				_unp = info?.UNP ?? string.Empty;
				_legalType = info?.LegalType ?? string.Empty;
				_bankNumber = info?.BankAccount.BankNumber ?? string.Empty;
				_bic = info?.BankAccount.BIC ?? string.Empty;
				_photo = info?.Photo ?? string.Empty;
				_rebate = EditingCompany.Rebate;
				_location = EditingCompany.Location;
			}
		}

		_wasOpen = IsOpen;
	}

	private void ResetForm()
	{
		_name = string.Empty;
		_address = string.Empty;
		_corporateEmail = string.Empty;
		_unp = string.Empty;
		_legalType = string.Empty;
		_bankNumber = string.Empty;
		_bic = string.Empty;
		_photo = string.Empty;
		_rebate = 0;
		_location = null;
		_errorMessage = string.Empty;
	}

	private void HandleLocationChanged(Location location)
	{
		_location = location;
	}

	private bool IsValid =>
		!string.IsNullOrWhiteSpace(_name) &&
		!string.IsNullOrWhiteSpace(_address) &&
		!string.IsNullOrWhiteSpace(_corporateEmail) &&
		!string.IsNullOrWhiteSpace(_unp) &&
		!string.IsNullOrWhiteSpace(_legalType) &&
		!string.IsNullOrWhiteSpace(_bankNumber) &&
		!string.IsNullOrWhiteSpace(_bic) &&
		_location is not null;

	private async Task HandleSubmit()
	{
		if (!IsValid)
		{
			_errorMessage = "Please fill in all required fields";
			return;
		}

		if (_location is null)
		{
			_errorMessage = "Please select a location on the map";
			return;
		}

		CompanySubmit submit = EditingCompany is { } editing
			? new CompanyUpdate(editing.Id, new UpdateCompany(_location, _photo, _name, _address, _corporateEmail, _rebate))
			: new CreateCompany(_location, new BankAccount(_bankNumber, _bic), _photo, _name, _address, _corporateEmail, _unp, _legalType, _rebate);

		await OnSubmit.InvokeAsync(submit);
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}
}
