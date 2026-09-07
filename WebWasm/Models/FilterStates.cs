namespace WebWasm.Models;

public enum UserKindFilter { All, DriversOnly, UsersOnly }
public enum ActiveUserFilter { All, ActiveOnly, InactiveOnly }
public enum VerifiedUserFilter { All, VerifiedOnly, UnverifiedOnly }

public record UsersFilterState(
	UserKindFilter UserKindFilter,
	List<RoleType> FilterRoles,
	ActiveUserFilter ActiveFilter,
	bool ShowCustomersOnly,
	VerifiedUserFilter VerifiedFilter,
	string SearchText = "",
	List<Guid>? FilterCompanyIds = null);

public record OrdersFilterState(
	List<OrderStatus> SelectedStatuses,
	double? MinWeight,
	double? MaxWeight,
	DateOnly? FromDate,
	DateOnly? ToDate,
	string SearchText = "");

// Home dashboard: фильтр периода для turnover/profit. Preset: all | 1w | 2w | 1m | 2m | 1q | custom.
// To — ВКЛЮЧИТЕЛЬНАЯ последняя дата (на API уходит to+1day exclusive). Для preset ≠ custom даты
// пересчитываются от текущей при каждой загрузке страницы.
public record TurnoverFilterState(string Preset, DateOnly? From, DateOnly? To);
