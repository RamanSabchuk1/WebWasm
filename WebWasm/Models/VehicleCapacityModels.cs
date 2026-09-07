namespace WebWasm.Models;

/// <summary>
/// SA read-model тоннажа машины (S5.0; контракт backend GET /admin/vehicle-capacities,
/// camelCase JSON). DefaultMin/MaxPrice — дефолтные цены для fallback в расчёте (0/0 = не настроено).
/// </summary>
public record VehicleCapacity(int WeightKg, decimal DefaultMinPrice, decimal DefaultMaxPrice, bool IsActive, DateTime Updated);

/// <summary>Тело POST /admin/vehicle-capacities (S5.0).</summary>
public record CreateVehicleCapacityRequest(int WeightKg, decimal DefaultMinPrice, decimal DefaultMaxPrice);

/// <summary>Тело PUT /admin/vehicle-capacities/{weightKg} (S5.0).</summary>
public record UpdateVehicleCapacityRequest(decimal DefaultMinPrice, decimal DefaultMaxPrice, bool IsActive);

/// <summary>
/// Публичный read-model тоннажа (контракт backend GET /vehicle-capacities, любой auth).
/// Используется для выбора Capacity при создании машины — значения задаёт только SA.
/// </summary>
public record VehicleCapacityOption(int WeightKg);
