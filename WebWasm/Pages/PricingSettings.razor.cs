using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

/// <summary>
/// SA-страница настроек ценообразования (pricing overhaul, S5.1, паттерн D32).
/// Backend: GET/PUT /admin/pricing-settings (S4.1). Глобальные значения + override по
/// Company/MaterialType; приоритет цепочки показан в UI (Company → MaterialType → Global).
/// Списки компаний/материалов — через CashService (паттерн проекта).
/// </summary>
public partial class PricingSettings
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private const string ScopeGlobal = "Global";
	private const string ScopeCompany = "Company";
	private const string ScopeMaterialType = "MaterialType";

	private static readonly string[] Scopes = [ScopeGlobal, ScopeCompany, ScopeMaterialType];

	private enum Tab
	{
		Settings,
		Simulator,
	}

	private Tab _tab = Tab.Settings;
	private string _scope = ScopeGlobal;
	private Guid? _scopeId;
	private Guid? _companyId;
	private Guid? _materialTypeId;

	private Company[] _companies = [];
	private MaterialType[] _materialTypes = [];
	private List<PricingSettingItem> _items = [];

	private bool _showEditDialog;
	private PricingSettingItem? _editItem;
	private decimal _editValue;
	private string? _editError;

	private string? EditTitle => _editItem is { } item ? T.Pricing.EditTitle(item.Key, T.Pricing.ScopeName(_scope)) : null;

	/// <summary>На кого действует override ключа на уровне компании; null — backend не прислал роли (старая версия).</summary>
	private string? CompanyEffect(PricingSettingItem item) => item.CompanyRoles switch
	{
		null => null,
		[] => T.Pricing.NoCompanyEffect,
		var roles => string.Join(", ", roles.Select(T.Pricing.RoleEffect)),
	};

	protected override async Task OnInitializedAsync()
	{
		await LoadItems();
	}

	private async Task ChangeScope(string scope)
	{
		_scope = scope;
		_items = [];

		if (scope == ScopeCompany && _companies.Length == 0)
		{
			_companies = await CashService.GetData<Company>();
		}
		else if (scope == ScopeMaterialType && _materialTypes.Length == 0)
		{
			_materialTypes = await CashService.GetData<MaterialType>();
		}

		_scopeId = scope switch
		{
			ScopeCompany => _companyId,
			ScopeMaterialType => _materialTypeId,
			_ => null,
		};
		await LoadItems();
	}

	private async Task OnCompanyChanged(ChangeEventArgs e)
	{
		_companyId = Guid.TryParse(e.Value?.ToString(), out var id) ? id : null;
		_scopeId = _companyId;
		await LoadItems();
	}

	private async Task OnMaterialTypeChanged(ChangeEventArgs e)
	{
		_materialTypeId = Guid.TryParse(e.Value?.ToString(), out var id) ? id : null;
		_scopeId = _materialTypeId;
		await LoadItems();
	}

	private async Task LoadItems()
	{
		if (_scope != ScopeGlobal && _scopeId is null)
		{
			_items = [];
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			var query = _scopeId is null
				? $"admin/pricing-settings?scope={_scope}"
				: $"admin/pricing-settings?scope={_scope}&scopeId={_scopeId}";
			var items = await ApiClient.Get<PricingSettingItem[]>(query);
			_items = [.. items.OrderBy(x => x.Key)];
		}, T.Pricing.LoadFailed);
	}

	private void OpenEditDialog(PricingSettingItem item)
	{
		_editItem = item;
		_editValue = item.OverrideValue ?? item.EffectiveValue ?? item.Default ?? 0;
		_editError = null;
		_showEditDialog = true;
	}

	private void CloseEditDialog() => _showEditDialog = false;

	private async Task SubmitEdit()
	{
		if (_editItem is not { } item)
		{
			return;
		}

		var key = item.Key;
		if ((item.Min is { } min && _editValue < min) || (item.Max is { } max && _editValue > max))
		{
			_editError = T.Pricing.RangeError(T.Pricing.Range(item.Min, item.Max));
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Put("admin/pricing-settings", new SetPricingSettingRequest(_scope, _scopeId, key, _editValue));
				ToastService.ShowSuccess(T.Pricing.Updated(key));
				_showEditDialog = false;
				await LoadItems();
			}
			catch (Exception ex)
			{
				_editError = T.Pricing.SaveFailed(ex.Message);
			}
		});
	}

	private async Task ResetOverride(PricingSettingItem item)
	{
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				// Value = null → backend удаляет override, значение снова наследуется по цепочке.
				await ApiClient.Put("admin/pricing-settings", new SetPricingSettingRequest(_scope, _scopeId, item.Key, null));
				ToastService.ShowSuccess(T.Pricing.ResetDone(item.Key));
				await LoadItems();
			}
			catch (Exception ex)
			{
				ToastService.ShowError(T.Pricing.ResetFailed(item.Key, ex.Message));
			}
		});
	}
}
