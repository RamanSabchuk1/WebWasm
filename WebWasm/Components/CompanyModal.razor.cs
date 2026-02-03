using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class CompanyModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<CreateCompany> OnSubmit { get; set; }

	private string _legalName = string.Empty;
	private string _legalAddress = string.Empty;
	private string _corporateEmail = string.Empty;
	private string _unp = string.Empty;
	private string _legalType = string.Empty;
	private string _bankNumber = string.Empty;
	private string _bic = string.Empty;
	private string _photo = string.Empty;
	private Location? _location;
	private TimeOnly _startWorkingHours = new TimeOnly(8, 0);
	private TimeOnly _endWorkingHours = new TimeOnly(17, 0);
	private List<string> _contactPhones = [];
	private string _newPhone = string.Empty;
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
		_legalName = string.Empty;
		_legalAddress = string.Empty;
		_corporateEmail = string.Empty;
		_unp = string.Empty;
		_legalType = string.Empty;
		_bankNumber = string.Empty;
		_bic = string.Empty;
		_photo = string.Empty;
		_location = null;
		_startWorkingHours = new TimeOnly(8, 0);
		_endWorkingHours = new TimeOnly(17, 0);
		_contactPhones = [];
		_newPhone = string.Empty;
		_errorMessage = string.Empty;
	}

	private void HandleLocationChanged(Location location)
	{
		_location = location;
	}

	private void AddPhone()
	{
		if (!string.IsNullOrWhiteSpace(_newPhone))
		{
			_contactPhones.Add(_newPhone.Trim());
			_newPhone = string.Empty;
		}
	}

	private void RemovePhone(int index)
	{
		if (index >= 0 && index < _contactPhones.Count)
		{
			_contactPhones.RemoveAt(index);
		}
	}

	private bool IsValid =>
		!string.IsNullOrWhiteSpace(_legalName) &&
		!string.IsNullOrWhiteSpace(_legalAddress) &&
		!string.IsNullOrWhiteSpace(_corporateEmail) &&
		!string.IsNullOrWhiteSpace(_unp) &&
		!string.IsNullOrWhiteSpace(_legalType) &&
		!string.IsNullOrWhiteSpace(_bankNumber) &&
		!string.IsNullOrWhiteSpace(_bic) &&
		_location is not null &&
		_startWorkingHours < _endWorkingHours;

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

		// Convert TimeOnly to TimeSpan
		var startWorkingHours = _startWorkingHours.ToTimeSpan();
		var endWorkingHours = _endWorkingHours.ToTimeSpan();

		var createCompany = new CreateCompany(
			_location,
			bankAccount,
			startWorkingHours,
			endWorkingHours,
			_photo,
			_contactPhones,
			_legalName,
			_legalAddress,
			_corporateEmail,
			_unp,
			_legalType
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
