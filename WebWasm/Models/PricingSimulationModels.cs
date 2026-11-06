namespace WebWasm.Models;

// Калькулятор цен (вкладка «Калькуляция» на странице Pricing Settings).
// Контракт backend: GET admin/pricing-settings/simulation/options, POST admin/pricing-settings/simulation.
// Enum backend приходят строками: Scope — "Global"/"MaterialType"/"Company" (null — дефолт ключа),
// Column — "Material"/"DeliveryOnCreate"/"DeliveryForCarrier"/"CommissionNoCompany"/"CommissionCompany"/"SlotsOnly",
// Role — "None"/"Buyer"/"Supplier"/"Carrier", PathMode — "Duration"/"Distance".

/// <summary>Значение, подставленное SA в калькуляторе «как если бы сохранил» на скоупе. В БД не пишется.</summary>
public record PricingSettingOverride(string Scope, Guid? ScopeId, string Key, decimal Value);

/// <summary>
/// Ось X графика — доля пути по городу (0…1), её задаёт backend. WeightTons — вес заказа, PathValue — минуты или км по PathMode.
/// Null в цене материала, rebate и VolumePriceProgression — взять сохранённые значения.
/// </summary>
public record PricingSimulationInput(
	Guid LoadingPlaceId,
	Guid RegionId,
	Guid? BuyerCompanyId,
	Guid? CarrierCompanyId,
	double WeightTons,
	string PathMode,
	double PathValue,
	decimal? MaterialCostPerTon,
	double? SupplierRebatePercent,
	double? CarrierRebatePercent,
	decimal? VolumePriceProgression,
	PricingSettingOverride[] Overrides,
	uint[] CustomDeliveries,
	PricingSimulationRegionPriceOverride[] RegionPriceOverrides);

/// <summary>Цена тоннажа в таблице региона, подставленная в памяти. Level — "City" или "Neighborhood".</summary>
public record PricingSimulationRegionPriceOverride(string Level, uint WeightKg, decimal Min, decimal Max);

public record PricingSimulationSource(string? Scope, Guid? ScopeId, string Name, bool IsOverride);

public record PricingSimulationEditTarget(string Scope, Guid? ScopeId, string Name);

public record PricingSimulationCell(
	string Column,
	string Role,
	decimal? Value,
	PricingSimulationSource Source,
	decimal? SavedValue,
	PricingSimulationSource SavedSource,
	PricingSimulationEditTarget[] EditTargets,
	string? Note);

public record PricingSimulationSetting(string Key, string Description, decimal? Min, decimal? Max, PricingSimulationCell[] Cells);

public record PricingSimulationRanges(
	double MinKm,
	double MaxKm,
	double KmLimit,
	double MinMinutes,
	double MaxMinutes,
	double MinutesLimit,
	double MinTons,
	double MaxTons,
	double SpeedCityKmh,
	double SpeedKmh,
	double Circuity,
	int VolumeMaxDeliveries);

public record PricingSimulationSlot(double RoundTripFactor, double TrafficBuffer, double LoadingMinutes, double PreparationMinutes, int MinSlotMinutes);

public record PricingSimulationPoint(
	double TMode,
	double Km,
	double Minutes,
	decimal Total,
	decimal Material,
	decimal Delivery,
	decimal Commission,
	decimal TotalLow,
	decimal TotalHigh,
	decimal? CarrierDelivery,
	decimal? CarrierCommission,
	decimal[]? DeliveryPrices,
	decimal[]? CarrierDeliveryPrices);

/// <summary>WeightKg = 0 — заказ из разных грузовиков.</summary>
public record PricingSimulationSeries(
	uint WeightKg,
	bool BuyerHasCompany,
	int Deliveries,
	double Tons,
	double VolumeFactor,
	decimal VolumeMultiplier,
	PricingSimulationPoint[] Points,
	string? Error);

public record PricingSimulationPrice(decimal Min, decimal Max, bool FromDefault, bool IsOverride);

public record PricingSimulationRegionPrice(uint WeightKg, PricingSimulationPrice? City, PricingSimulationPrice? Neighborhood);

public record PricingSimulationResult(
	PricingSimulationSetting[] Settings,
	PricingSimulationRanges Ranges,
	PricingSimulationSlot Slot,
	PricingSimulationRegionPrice[] RegionPrices,
	decimal MaterialCostPerTon,
	double SupplierRebatePercent,
	double CarrierRebatePercent,
	decimal VolumePriceProgression,
	PricingSimulationSeries[] Series);

public record PricingSimulationLoadingPlace(Guid Id, string Name, decimal CostPerTon, string? MaterialName, Guid CompanyId, string CompanyName, string ProducerName, Guid RegionId);

public record PricingSimulationCompany(Guid Id, string Name, bool IsCarrier, bool IsSupplier, double Rebate);

public record PricingSimulationRegion(Guid Id, string Name, string[] Levels);

public record PricingSimulationOptions(
	PricingSimulationLoadingPlace[] LoadingPlaces,
	PricingSimulationCompany[] Companies,
	PricingSimulationRegion[] Regions,
	uint[] Weights);
