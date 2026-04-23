using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class CompanyModal : ComponentBase
{
    [Parameter] public Company? EditingCompany { get; set; }
    [Parameter] public bool IsOpen { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<(CreateCompany? Create, UpdateCompany? Update, Guid CompanyId)> OnSubmit { get; set; }
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
	private LocationMapPicker? _locationPicker;

	protected override void OnParametersSet()
	{
		if (IsOpen)
		{
            if (EditingCompany is not null)
            {
                _name = EditingCompany.Name;
				_address = EditingCompany.CompanyInfo!.Address;
				_corporateEmail = EditingCompany.CompanyInfo!.CorporateEmail;
				_unp = EditingCompany.CompanyInfo!.UNP;
				_legalType = EditingCompany.CompanyInfo!.LegalType;
				_bankNumber = EditingCompany.CompanyInfo!.BankAccount.BankNumber;
				_bic = EditingCompany.CompanyInfo!.BankAccount.BIC;
				_photo = EditingCompany.CompanyInfo!.Photo;
                _rebate = EditingCompany.Rebate;
				_location = EditingCompany.Location;
            }
			else
			{
				ResetForm();
			}
		}
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

		var bankAccount = new BankAccount(_bankNumber, _bic);

		var createCompany = IsEditMode ? null : new CreateCompany(
			_location,
			bankAccount,
			_photo,
			_name,
			_address,
			_corporateEmail,
			_unp,
			_legalType,
			_rebate
		);

		var updateCompany = !IsEditMode ? null : new UpdateCompany(
            _location,
            _photo,
            _name,
            _address,
            _corporateEmail,
            _rebate
        );

		await OnSubmit.InvokeAsync((createCompany, updateCompany, EditingCompany?.Id ?? default));
		ResetForm();
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}
}
