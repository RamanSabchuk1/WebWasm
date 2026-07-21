namespace WebWasm.Models;

// Синхронизирован с backend-каноном Kliffort.Contracts/Security/DataSecurityLevel.cs (backend = source of truth, D43).
// Backend сериализует enum как СТРОКУ (JsonStringEnumConverter, имена значений PascalCase),
// frontend десериализует строкой (UseStringEnumConverter=true в AppJsonSerializerContext) — имена ДОЛЖНЫ совпадать.
public enum DataSecurityLevel : byte
{
	Public = 0,
	Internal = 1,
	CompanyOperational = 2,
	Restricted = 3,
	Sensitive = 4,
	SystemOnly = 5
}

public enum EncryptionPurpose
{
	Data,
	BlindIndex
}

public record SecurityLevelRequest(DataSecurityLevel Level);
public record EncryptionKeyInfo(Guid Id, EncryptionPurpose Purpose, int Version, bool IsActive, DateTime CreatedAt, DateTime? FullyMigratedAt);

/// <summary>
/// Результат POST Admin/secure-data/backfill (см. Kliffort.Contracts.Services.BackfillResult).
/// CompanyRootProcessed — MR review: отдельный счётчик для корневого Company (rebate/location),
/// не путать с CompaniesProcessed (CompanyInfo: address/unp/bank).
/// </summary>
public record BackfillResult(int UsersProcessed, int CompaniesProcessed, int VehiclesProcessed, int CompanyRootProcessed);
