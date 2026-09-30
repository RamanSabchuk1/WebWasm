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
	private Company? _editingCompany = null;
	private bool _isCompanyModalOpen = false;
	private readonly ConfirmState _confirm = new();

	protected override async Task OnInitializedAsync()
	{
		await LoadCompanies(true);
	}

	private void HandleEditCompany(Company company)
	{
		_editingCompany = company;
		_isCompanyModalOpen = true;
	}

	private Company? _securityLevelTarget;

	private async Task LoadCompanies(bool useCash)
	{
		_companies = [.. await CashService.GetData<Company>(useCash)];
	}

	private void OpenAddCompanyModal()
	{
		_editingCompany = null;
		_isCompanyModalOpen = true;
	}

	private void CloseCompanyModal()
	{
		_isCompanyModalOpen = false;
		_editingCompany = null;
	}

	private async Task HandleCompanySubmit(CompanySubmit submit)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			switch (submit)
			{
				case CreateCompany create:
					await ApiClient.Post("Companies", create);
					ToastService.ShowSuccess("Company created successfully!");
					break;
				case CompanyUpdate(var companyId, var update):
					await ApiClient.Put($"Companies/{companyId}", update);
					ToastService.ShowSuccess("Company updated successfully!");
					break;
			}

			await LoadCompanies(false);
			CloseCompanyModal();
		}, "Failed to call API with company: ");
	}

	private void HandleDeleteCompany(Company company)
	{
		_confirm.Ask("Soft Delete Company", $"Are you sure you want to soft-delete company '{company.Name}'? This will also cascade soft-delete all related Producers, Vehicles, Drivers and Users belonging to this company. This action cannot be easily undone.", async () => await DeleteCompanyConfirmed(company.Id));
	}

	private async Task DeleteCompanyConfirmed(Guid companyId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Admin/company/{companyId}");
			ToastService.ShowSuccess("Company soft-deleted successfully!");
			await LoadCompanies(false);
		}, "Failed to delete company: ");
	}

	private void HandleToggleActive((Guid CompanyId, bool IsActive) data)
	{
		var company = _companies.FirstOrDefault(c => c.Id == data.CompanyId);
		if (company is null)
		{
			return;
		}

		var action = data.IsActive ? "activate" : "deactivate";
		_confirm.Ask($"Confirm {action.ToUpper()} Company", $"Are you sure you want to {action} company '{company.Name}'?", async () => await ToggleActiveConfirmed(data.CompanyId, data.IsActive));
	}

	private async Task ToggleActiveConfirmed(Guid companyId, bool isActive)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Companies/{companyId}/active?isActive={isActive.ToString().ToLower()}");
			ToastService.ShowSuccess($"Company {(isActive ? "activated" : "deactivated")} successfully!");
			await LoadCompanies(false);
		}, "Failed to update company status: ");
	}
}
