using Microsoft.AspNetCore.Components;
using WebWasm.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Companies
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private List<Company> _companies = [];
	private Company? _editingCompany = null;
	private bool _isCompanyModalOpen = false;
	private readonly ConfirmState _confirm = new();
	// The confirm verb and colour depend on the action: delete is red, activate/deactivate is the main button.
	private string? _confirmText;
	private bool _confirmDangerous = true;

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
					ToastService.ShowSuccess(T.Companies.Created);
					break;
				case CompanyUpdate(var companyId, var update):
					await ApiClient.Put($"Companies/{companyId}", update);
					ToastService.ShowSuccess(T.Companies.Updated);
					break;
			}

			await LoadCompanies(false);
			CloseCompanyModal();
		}, T.Companies.SaveFailed);
	}

	private void HandleDeleteCompany(Company company)
	{
		_confirmText = null;
		_confirmDangerous = true;
		_confirm.Ask(T.Companies.DeleteTitle, T.Companies.DeleteMessage(company.Name), async () => await DeleteCompanyConfirmed(company.Id));
	}

	private async Task DeleteCompanyConfirmed(Guid companyId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Admin/company/{companyId}");
			ToastService.ShowSuccess(T.Companies.DeletedToast);
			await LoadCompanies(false);
		}, T.Companies.DeleteFailed);
	}

	private void HandleToggleActive((Guid CompanyId, bool IsActive) data)
	{
		var company = _companies.FirstOrDefault(c => c.Id == data.CompanyId);
		if (company is null)
		{
			return;
		}

		_confirmText = data.IsActive ? T.Companies.Activate : T.Companies.Deactivate;
		_confirmDangerous = false;
		_confirm.Ask(
			data.IsActive ? T.Companies.ActivateTitle : T.Companies.DeactivateTitle,
			data.IsActive ? T.Companies.ActivateMessage(company.Name) : T.Companies.DeactivateMessage(company.Name),
			async () => await ToggleActiveConfirmed(data.CompanyId, data.IsActive));
	}

	private async Task ToggleActiveConfirmed(Guid companyId, bool isActive)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Companies/{companyId}/active?isActive={isActive.ToString().ToLower()}");
			ToastService.ShowSuccess(isActive ? T.Companies.ActivatedToast : T.Companies.DeactivatedToast);
			await LoadCompanies(false);
		}, T.Companies.StatusFailed);
	}
}
