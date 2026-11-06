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
public partial class PricingSettings : ComponentBase
{
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private enum Tab
	{
		Settings,
		Simulator,
	}

	private Tab _tab = Tab.Settings;
	private string _scope = "Global";
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

	private static readonly Dictionary<string, string> RoleNames = new()
	{
		["Buyer"] = "покупатель → комиссия",
		["Supplier"] = "поставщик → материал",
		["Carrier"] = "перевозчик → доставка",
	};

	/// <summary>На кого действует override ключа на уровне компании; null — backend не прислал роли (старая версия).</summary>
	private static string? CompanyEffect(PricingSettingItem item) => item.CompanyRoles switch
	{
		null => null,
		[] => "на компании не действует",
		var roles => string.Join(", ", roles.Select(r => RoleNames.GetValueOrDefault(r, r))),
	};

	private static string Range(PricingSettingItem item) => (item.Min, item.Max) switch
	{
		(null, null) => "—",
		({ } min, null) => $"≥ {min:0.####}",
		(null, { } max) => $"≤ {max:0.####}",
		({ } min, { } max) => $"{min:0.####} … {max:0.####}",
	};

	protected override async Task OnInitializedAsync()
	{
		await LoadItems();
	}

	private async Task ChangeScope(string scope)
	{
		_scope = scope;
		_items = [];

		if (scope == "Company" && _companies.Length == 0)
		{
			_companies = await CashService.GetData<Company>();
		}
		else if (scope == "MaterialType" && _materialTypes.Length == 0)
		{
			_materialTypes = await CashService.GetData<MaterialType>();
		}

		_scopeId = scope switch
		{
			"Company" => _companyId,
			"MaterialType" => _materialTypeId,
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
		if (_scope != "Global" && _scopeId is null)
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
		}, "Failed to load pricing settings: ");
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
			_editError = $"Допустимый диапазон: {Range(item)}";
			return;
		}

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Put("admin/pricing-settings", new SetPricingSettingRequest(_scope, _scopeId, key, _editValue));
				ToastService.ShowSuccess($"Setting {key} updated.");
				_showEditDialog = false;
				await LoadItems();
			}
			catch (Exception ex)
			{
				_editError = ex.Message;
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
				ToastService.ShowSuccess($"Override {item.Key} reset to inherited.");
				await LoadItems();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to reset {item.Key}: {ex.Message}");
			}
		});
	}
}
