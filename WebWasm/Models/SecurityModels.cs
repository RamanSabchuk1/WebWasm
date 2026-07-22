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

// Тело запроса POST users/{userInfoId}/passport (SuperAdmin-only, backend = source of truth, D43).
// Точное соответствие backend Kliffort.WebApi.Controllers.SetPassportRequest
// (Kliffort.WebApi/Controllers/UsersController.cs): те же имена и порядок полей.
// Все поля опциональны (backend требует хотя бы одно непустое); переданные значения
// шифруются на backend (AES-256-GCM) и кладутся в AdditionalSecureInfo (Sensitive-уровень).
public record SetPassportRequest(string? Number, string? IdentificationNumber, string? IssuedBy, DateOnly? IssuedDate);

/// <summary>
/// Результат POST Admin/secure-data/backfill (см. Kliffort.Contracts.Services.BackfillResult).
/// CompanyRootProcessed — MR review: отдельный счётчик для корневого Company (rebate/location),
/// не путать с CompaniesProcessed (CompanyInfo: address/unp/bank).
/// </summary>
public record BackfillResult(int UsersProcessed, int CompaniesProcessed, int VehiclesProcessed, int CompanyRootProcessed);
