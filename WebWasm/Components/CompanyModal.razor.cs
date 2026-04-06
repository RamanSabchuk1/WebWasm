using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class CompanyModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<CreateCompany> OnSubmit { get; set; }

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
			ResetForm();
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

		var createCompany = new CreateCompany(
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

		await OnSubmit.InvokeAsync(createCompany);
		ResetForm();
	}

	private async Task CloseModal()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}
}
