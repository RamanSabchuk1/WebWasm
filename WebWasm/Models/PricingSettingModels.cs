namespace WebWasm.Models;

/// <summary>
/// Элемент настроек ценообразования (S5.1; контракт backend GET /admin/pricing-settings,
/// camelCase JSON). AllowedScopes — строки ("Global"/"Company"/"MaterialType"), backend шлёт
/// enum строками (JsonStringEnumConverter). OverrideValue = null → наследуется по цепочке
/// (приоритет: Company → MaterialType → Global → Default). EffectiveValue = null → ключ не задан
/// нигде и дефолта нет (незасеянные ключи — обратная совместимость).
/// </summary>
public record PricingSettingItem(
	string Key,
	string Description,
	string[] AllowedScopes,
	decimal? Default,
	decimal? OverrideValue,
	decimal? EffectiveValue,
	DateTime? Updated,
	Guid? UpdatedBy);

/// <summary>Тело PUT /admin/pricing-settings (S5.1). Value = null → сброс override (наследование).</summary>
public record SetPricingSettingRequest(string Scope, Guid? ScopeId, string Key, decimal? Value);
