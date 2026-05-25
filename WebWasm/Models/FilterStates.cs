using WebWasm.Models;

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
	string SearchText = "");

public record OrdersFilterState(
	List<OrderStatus> SelectedStatuses,
	double? MinWeight,
	double? MaxWeight,
	DateOnly? FromDate,
	DateOnly? ToDate,
	string SearchText = "");
