namespace WebWasm.Models;

public enum DataSecurityLevel
{
	Public,
	Internal,
	CompanyOperational,
	Restricted,
	Sensitive,
	SystemOnly
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
