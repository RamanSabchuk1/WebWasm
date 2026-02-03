using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Companies : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<Company> _companies = [];
	private CompaniesTable? _companiesTableRef;
	private bool _isCompanyModalOpen = false;
	private bool _showConfirmDialog = false;
	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task>? _confirmAction = null;

	protected override async Task OnInitializedAsync()
	{
		await LoadCompanies(true);
	}

	private async Task LoadCompanies(bool useCash)
	{
		_companies = [.. await CashService.GetData<Company>(useCash)];
	}

	private void OpenAddCompanyModal()
	{
		_isCompanyModalOpen = true;
	}

	private void CloseCompanyModal()
	{
		_isCompanyModalOpen = false;
	}

	private async Task HandleCompanySubmit(CreateCompany createCompany)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post("Companies", createCompany);
				ToastService.ShowSuccess("Company created successfully!");
				await LoadCompanies(false);
				CloseCompanyModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create company: {ex.Message}");
			}
		});
	}

	private void HandleToggleActive((Guid CompanyId, bool IsActive) data)
	{
		var company = _companies.FirstOrDefault(c => c.Id == data.CompanyId);
		if (company is null)
			return;

		var action = data.IsActive ? "activate" : "deactivate";
		_confirmTitle = $"Confirm {action.ToUpper()} Company";
		_confirmMessage = $"Are you sure you want to {action} company '{company.LegalName}'?";
		_confirmAction = async () => await ToggleActiveConfirmed(data.CompanyId, data.IsActive);
		_showConfirmDialog = true;
	}

	private async Task ToggleActiveConfirmed(Guid companyId, bool isActive)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Companies/{companyId}/active?isActive={isActive.ToString().ToLower()}");
				ToastService.ShowSuccess($"Company {(isActive ? "activated" : "deactivated")} successfully!");
				await LoadCompanies(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to update company status: {ex.Message}");
			}
		});
	}

	private void CloseConfirmDialog()
	{
		_showConfirmDialog = false;
		_confirmTitle = string.Empty;
		_confirmMessage = string.Empty;
		_confirmAction = null;
	}

	private async Task HandleConfirm()
	{
		_showConfirmDialog = false;
		if (_confirmAction is not null)
		{
			await _confirmAction.Invoke();
		}
		CloseConfirmDialog();
	}
}
