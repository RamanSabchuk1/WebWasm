using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Components;
using WebWasm.Helpers;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Users : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;
	[Inject] private LocalStorageService LocalStorage { get; set; } = default!;

	private User[]? _users;
	private Driver[] _drivers = [];
	private Company[] _companies = [];
	private DriverSlot[] _driverSlots = [];
	private Dictionary<Guid, Driver> _driverByUserInfoId = [];
	private Dictionary<Guid, List<DriverSlot>> _slotsByDriverId = [];
	private readonly HashSet<Guid> _expandedUsers = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };

	private bool _showCreateUserModal;
	private bool _showCreateDriverModal;
	private bool _showDriverSlotModal;
	private bool _showRolesModal;

	private Guid? _selectedCompanyId;
	private string _userFirstName = string.Empty;
	private string _userMiddleName = string.Empty;
	private string _userLastName = string.Empty;
	private string _userMobilePhone = string.Empty;
	private readonly HashSet<RoleType> _selectedRoles = [];
	private string _userErrorMessage = string.Empty;

	private Guid? _selectedDriverCompanyId;
	private string _driverFirstName = string.Empty;
	private string _driverMiddleName = string.Empty;
	private string _driverLastName = string.Empty;
	private string _driverMobilePhone = string.Empty;
	private string _driverPhoto = string.Empty;
	private string _driverErrorMessage = string.Empty;

	private Guid? _slotDriverId;
	private Guid? _slotCompanyId;
	private DateOnly _slotDate = DateOnly.FromDateTime(DateTime.Today);
	private TimeOnly _slotStartTime = new(8, 0);
	private TimeOnly _slotEndTime = new(17, 0);
	private readonly List<CreateDriverSlot> _newSlots = [];
	private string _slotErrorMessage = string.Empty;

	private User? _rolesTargetUser;
	private readonly HashSet<RoleType> _roleEditSelection = [];
	private string _rolesErrorMessage = string.Empty;

	private string _searchText = string.Empty;
	private readonly List<RoleType> _filterRoles = [];
	private readonly List<Guid> _filterCompanyIds = [];
	private UserKindFilter _userKindFilter = UserKindFilter.All;
	private ActiveUserFilter _activeUserFilter = ActiveUserFilter.All;
	private bool _showCustomersOnly = false;
	private VerifiedUserFilter _verifiedFilter = VerifiedUserFilter.All;
	private bool _showFilters = false;

	private readonly ConfirmState _confirm = new();

	private bool _showAssignCompanyModal = false;
	private User? _assignCompanyTargetUser;
	private Guid _assignCompanyId = Guid.Empty;
	private string _assignCompanyError = string.Empty;

	private User? _securityLevelTarget;

	private bool _showPassportModal;
	private User? _passportTargetUser;
	private string _passportNumber = string.Empty;
	private string _passportIdentificationNumber = string.Empty;
	private string _passportIssuedBy = string.Empty;
	private DateOnly _passportIssuedDate = default;
	private string _passportErrorMessage = string.Empty;

	private void OpenPassportModal(User user)
	{
		_passportTargetUser = user;
		_passportNumber = string.Empty;
		_passportIdentificationNumber = string.Empty;
		_passportIssuedBy = string.Empty;
		_passportIssuedDate = default;
		_passportErrorMessage = string.Empty;
		_showPassportModal = true;
	}

	private void ClosePassportModal()
	{
		_showPassportModal = false;
		_passportTargetUser = null;
		_passportNumber = string.Empty;
		_passportIdentificationNumber = string.Empty;
		_passportIssuedBy = string.Empty;
		_passportIssuedDate = default;
		_passportErrorMessage = string.Empty;
	}

	private async Task SubmitPassport()
	{
		if (_passportTargetUser is null)
		{
			return;
		}

		var number = string.IsNullOrWhiteSpace(_passportNumber) ? null : _passportNumber.Trim();
		var identificationNumber = string.IsNullOrWhiteSpace(_passportIdentificationNumber) ? null : _passportIdentificationNumber.Trim();
		var issuedBy = string.IsNullOrWhiteSpace(_passportIssuedBy) ? null : _passportIssuedBy.Trim();
		DateOnly? issuedDate = _passportIssuedDate == default ? null : _passportIssuedDate;

		if (number is null && identificationNumber is null && issuedBy is null && issuedDate is null)
		{
			_passportErrorMessage = "Please fill in at least one field.";
			return;
		}

		var userInfoId = _passportTargetUser.UserInfo.Id;
		var request = new SetPassportRequest(number, identificationNumber, issuedBy, issuedDate);

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"users/{userInfoId}/passport", request);
				ToastService.ShowSuccess("Passport data set successfully");
				ClosePassportModal();
				await LoadData(false);
			}
			catch (Exception ex)
			{
				_passportErrorMessage = $"Failed to set passport data: {ex.Message}";
			}
		});
	}

	private void OpenAssignCompanyModal(User user)
	{
		_assignCompanyTargetUser = user;
		_assignCompanyId = Guid.Empty;
		_assignCompanyError = string.Empty;
		_showAssignCompanyModal = true;
	}

	private void CloseAssignCompanyModal()
	{
		_showAssignCompanyModal = false;
		_assignCompanyTargetUser = null;
		_assignCompanyId = Guid.Empty;
		_assignCompanyError = string.Empty;
	}

	private async Task ConfirmAssignCompany()
	{
		if (_assignCompanyTargetUser is null || _assignCompanyId == Guid.Empty)
		{
			_assignCompanyError = "Please select a company.";
			return;
		}

		var userInfoId = _assignCompanyTargetUser.UserInfo.Id;
		var companyId = _assignCompanyId;
		CloseAssignCompanyModal();

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Admin/user/{userInfoId}/company/{companyId}");
			ToastService.ShowSuccess("User assigned to company successfully");
			await LoadData(false);
		}, "Failed to assign company: ");
	}

	private void RequestUnassignCompany(User user)
	{
		var name = GetDisplayName(user.UserInfo);
		_confirm.Ask("Unassign Company", $"Unassign '{name}' from their current company? Their CompanyId will be cleared.", async () => await UnassignCompanyConfirmed(user.UserInfo.Id));
	}

	private async Task UnassignCompanyConfirmed(Guid userInfoId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Admin/user/{userInfoId}/company");
			ToastService.ShowSuccess("User unassigned from company successfully");
			await LoadData(false);
		}, "Failed to unassign company: ");
	}

	private void RequestDeleteUserInfo(User user)
	{
		var name = GetDisplayName(user.UserInfo);
		_confirm.Ask("Soft Delete User", $"Soft-delete user '{name}' (login: {user.Login})? This cascades to the driver record (if any) and all their vehicles. This action cannot be easily undone.", async () => await DeleteUserInfoConfirmed(user.UserInfo.Id));
	}

	private void RequestResetPassword(User user)
	{
		var name = GetDisplayName(user.UserInfo);
		_confirm.Ask("Reset Password", $"Reset password for '{name}' (login: {user.Login})? A new password will be generated and sent to the user via SMS. The old password will stop working immediately.", async () => await ResetPasswordConfirmed(user.Id));
	}

	private async Task ResetPasswordConfirmed(Guid userId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"users/{userId}/password-reset");
			ToastService.ShowSuccess("Password reset — the new password was sent to the user via SMS");
		}, "Failed to reset password: ");
	}

	private async Task DeleteUserInfoConfirmed(Guid userInfoId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Admin/user/{userInfoId}");
			ToastService.ShowSuccess("User soft-deleted successfully");
			await LoadData(false);
		}, "Failed to delete user: ");
	}

	private void RequestDeleteDriver(User user, Driver driver)
	{
		var name = GetDisplayName(user.UserInfo);
		_confirm.Ask("Soft Delete Driver", $"Soft-delete the driver record for '{name}'? This cascades to all their vehicles. The user account itself will remain.", async () => await DeleteDriverConfirmed(driver.Id));
	}

	private async Task DeleteDriverConfirmed(Guid driverId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Delete($"Admin/driver/{driverId}");
			ToastService.ShowSuccess("Driver soft-deleted successfully");
			await LoadData(false);
		}, "Failed to delete driver: ");
	}

	private void RequestCreateDriver(User user)
	{
		var name = GetDisplayName(user.UserInfo);
		_confirm.Ask("Create Driver", $"Create a driver record for '{name}' (login: {user.Login})?", async () => await CreateDriverForUserConfirmed(user.UserInfo.Id));
	}

	private async Task CreateDriverForUserConfirmed(Guid userInfoId)
	{
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Admin/user/{userInfoId}/driver", new PhotoDto(string.Empty));
			ToastService.ShowSuccess("Driver created successfully");
			await LoadData(false);
		}, "Failed to create driver: ");
	}

	private async Task AddFilterRole(ChangeEventArgs e)
	{
		if (Enum.TryParse<RoleType>(e.Value?.ToString(), out var role))
		{
			if (!_filterRoles.Contains(role))
			{
				_filterRoles.Add(role);
				_ = _pagination.SetCurrentPageIndexAsync(0);
				await SaveFilters();
			}
		}
	}

	private async Task RemoveFilterRole(RoleType role)
	{
		_filterRoles.Remove(role);
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task AddFilterCompany(ChangeEventArgs e)
	{
		if (Guid.TryParse(e.Value?.ToString(), out var companyId) && !_filterCompanyIds.Contains(companyId))
		{
			_filterCompanyIds.Add(companyId);
			_ = _pagination.SetCurrentPageIndexAsync(0);
			await SaveFilters();
		}
	}

	private async Task RemoveFilterCompany(Guid companyId)
	{
		_filterCompanyIds.Remove(companyId);
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private string GetCompanyName(Guid companyId) =>
		_companies.FirstOrDefault(c => c.Id == companyId)?.Name ?? companyId.ToString();

	private async Task OnSearchTextChanged(ChangeEventArgs e)
	{
		_searchText = e.Value?.ToString() ?? string.Empty;
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task OnUserKindFilterChanged(UserKindFilter value)
	{
		_userKindFilter = value;
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task OnActiveFilterChanged(ActiveUserFilter value)
	{
		_activeUserFilter = value;
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task OnVerifiedFilterChanged(VerifiedUserFilter value)
	{
		_verifiedFilter = value;
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task OnShowCustomersChanged(ChangeEventArgs e)
	{
		_showCustomersOnly = e.Value is bool b && b;
		_ = _pagination.SetCurrentPageIndexAsync(0);
		await SaveFilters();
	}

	private async Task SaveFilters()
	{
		var state = new UsersFilterState(_userKindFilter, [.. _filterRoles], _activeUserFilter, _showCustomersOnly, _verifiedFilter, _searchText ?? string.Empty, [.. _filterCompanyIds]);
		await LocalStorage.SetItemAsync("users_filters", state);
	}

	private int ActiveFilterCount =>
		_filterRoles.Count
		+ _filterCompanyIds.Count
		+ (_userKindFilter != UserKindFilter.All ? 1 : 0)
		+ (_activeUserFilter != ActiveUserFilter.All ? 1 : 0)
		+ (_showCustomersOnly ? 1 : 0)
		+ (_verifiedFilter != VerifiedUserFilter.All ? 1 : 0)
		+ (string.IsNullOrWhiteSpace(_searchText) ? 0 : 1);

	private User[] FilteredUsers
	{
		get
		{
			if (_users is null)
			{
				return [];
			}

			IEnumerable<User> filtered = _users;

			if (_userKindFilter == UserKindFilter.DriversOnly)
			{
				filtered = filtered.Where(u => GetDriver(u) is not null);
			}
			else if (_userKindFilter == UserKindFilter.UsersOnly)
			{
				filtered = filtered.Where(u => GetDriver(u) is null);
			}

			if (_filterRoles.Count > 0)
			{
				filtered = filtered.Where(u => u.Roles.Any(r => _filterRoles.Contains(r)));
			}

			if (_filterCompanyIds.Count > 0)
			{
				// Компания пользователя — через UserInfo.Company или Driver.UserInfo.Company (GetCompany).
				filtered = filtered.Where(u => GetCompany(u, GetDriver(u)) is { } company && _filterCompanyIds.Contains(company.Id));
			}

			if (_activeUserFilter == ActiveUserFilter.ActiveOnly)
			{
				filtered = filtered.Where(u => u.UserInfo.IsActive);
			}
			else if (_activeUserFilter == ActiveUserFilter.InactiveOnly)
			{
				filtered = filtered.Where(u => !u.UserInfo.IsActive);
			}

			if (_showCustomersOnly)
			{
				filtered = filtered.Where(u => u.Roles.Count == 0);
			}

			if (_verifiedFilter == VerifiedUserFilter.VerifiedOnly)
			{
				filtered = filtered.Where(u => u.UserVerified);
			}
			else if (_verifiedFilter == VerifiedUserFilter.UnverifiedOnly)
			{
				filtered = filtered.Where(u => !u.UserVerified);
			}

			if (!string.IsNullOrWhiteSpace(_searchText))
			{
				var query = _searchText.Trim();
				filtered = filtered.Where(u =>
					(u.UserInfo.FirstName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(u.UserInfo.MiddleName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(u.UserInfo.LastName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(u.UserInfo.MobilePhone?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
					(u.Login?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			return [.. filtered];
		}
	}

	private static readonly RoleType[] _roleOptions = Enum.GetValues<RoleType>();

	protected override async Task OnInitializedAsync()
	{
		await LoadData(true);

		var savedFilters = await LocalStorage.GetItemOrDefaultAsync<UsersFilterState?>("users_filters", null);
		if (savedFilters is not null)
		{
			_userKindFilter = savedFilters.UserKindFilter;
			_filterRoles.AddRange(savedFilters.FilterRoles ?? []);
			_filterCompanyIds.AddRange(savedFilters.FilterCompanyIds ?? []);
			_activeUserFilter = savedFilters.ActiveFilter;
			_showCustomersOnly = savedFilters.ShowCustomersOnly;
			_verifiedFilter = savedFilters.VerifiedFilter;
			_searchText = savedFilters.SearchText ?? string.Empty;
		}
	}

	private async Task LoadData(bool useCash)
	{
		(_users, _companies, _drivers) = await CashService.GetUsersCompaniesDrivers(useCash);
		_driverSlots = await CashService.GetSlots(_drivers, _users, _companies, useCash);
		BuildLookups();
	}

	private void BuildLookups()
	{
		_driverByUserInfoId = _drivers
			.Where(driver => driver.UserInfo is not null)
			.ToDictionary(driver => driver.UserInfo!.Id, driver => driver);

		_slotsByDriverId = _driverSlots
			.GroupBy(slot => slot.DriverId)
			.ToDictionary(
				group => group.Key,
				group => group.OrderBy(slot => slot.WorkingDay)
					.ThenBy(slot => slot.StartTime)
					.ToList());
	}

	private int TotalUsers => _users?.Length ?? 0;
	private int ActiveUsers => _users?.Count(user => user.UserInfo.IsActive) ?? 0;
	private int SuperAdminUsers => _users?.Count(user => user.Roles.Contains(RoleType.SuperAdmin)) ?? 0;
	private int DriverCount => _drivers.Length;

	private Driver? GetDriver(User user)
		=> _driverByUserInfoId.TryGetValue(user.UserInfo.Id, out var driver) ? driver : null;

	private static Company? GetCompany(User user, Driver? driver)
		=> user.UserInfo.Company ?? driver?.UserInfo?.Company;

	private List<DriverSlot> GetDriverSlots(Guid driverId)
		=> _slotsByDriverId.TryGetValue(driverId, out var slots) ? slots : [];

	private static string GetDisplayName(UserInfo? userInfo)
	{
		if (userInfo is null)
		{
			return "Unknown";
		}

		var parts = new[] { userInfo.FirstName, userInfo.MiddleName, userInfo.LastName }
			.Where(part => !string.IsNullOrWhiteSpace(part));
		var name = string.Join(" ", parts);
		return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
	}
	// Значение паспортного поля из secure-словаря UserInfo.Passport (ключи backend: passport.*).
	// Отсутствующий ключ → пустая строка. Значение (в т.ч. issuedDate) выводится КАК ЕСТЬ, без переформатирования.
	private static string GetPassportValue(IReadOnlyDictionary<string, string> passport, string key)
		=> passport.TryGetValue(key, out var value) ? value : string.Empty;

	private bool IsExpanded(Guid userId) => _expandedUsers.Contains(userId);

	private void ToggleExpanded(Guid userId) => _expandedUsers.Toggle(userId);

	private static string FormatSlot(DriverSlot slot) => FormatSlot(slot.WorkingDay, slot.StartTime, slot.EndTime);

	private static string FormatSlot(CreateDriverSlot slot) => FormatSlot(slot.WorkingDay, slot.StartTime, slot.EndTime);

	private static string FormatSlot(DateOnly day, TimeOnly start, TimeOnly end) =>
		$"{day:yyyy-MM-dd} {start:HH:mm} - {end:HH:mm}";

	private void OpenCreateUserModal()
	{
		_userErrorMessage = string.Empty;
		_showCreateUserModal = true;
	}

	private void CloseCreateUserModal()
	{
		_showCreateUserModal = false;
		_userErrorMessage = string.Empty;
		_selectedCompanyId = null;
		_userFirstName = string.Empty;
		_userMiddleName = string.Empty;
		_userLastName = string.Empty;
		_userMobilePhone = string.Empty;
		_selectedRoles.Clear();
	}

	private void OpenCreateDriverModal()
	{
		_driverErrorMessage = string.Empty;
		_showCreateDriverModal = true;
	}

	private void CloseCreateDriverModal()
	{
		_showCreateDriverModal = false;
		_driverErrorMessage = string.Empty;
		_selectedDriverCompanyId = null;
		_driverFirstName = string.Empty;
		_driverMiddleName = string.Empty;
		_driverLastName = string.Empty;
		_driverMobilePhone = string.Empty;
		_driverPhoto = string.Empty;
	}

	private void OpenDriverSlotModal(User user)
	{
		var driver = GetDriver(user);
		var company = GetCompany(user, driver);
		if (driver is null || company is null)
		{
			return;
		}

		SetInitialSlotTimes(GetDriverSlots(driver.Id));
		_slotDriverId = driver.Id;
		_slotCompanyId = company.Id;
		_slotErrorMessage = string.Empty;
		_showDriverSlotModal = true;
	}

	private void CloseDriverSlotModal()
	{
		_showDriverSlotModal = false;
		_slotErrorMessage = string.Empty;
		_slotDriverId = null;
		_slotCompanyId = null;
		_newSlots.Clear();
	}

	private void OpenRolesModal(User user)
	{
		_rolesTargetUser = user;
		_rolesErrorMessage = string.Empty;
		_roleEditSelection.Clear();
		foreach (var role in user.Roles.Distinct())
		{
			_roleEditSelection.Add(role);
		}

		_showRolesModal = true;
	}

	private void CloseRolesModal()
	{
		_showRolesModal = false;
		_rolesTargetUser = null;
		_rolesErrorMessage = string.Empty;
		_roleEditSelection.Clear();
	}

	private void ToggleRole(RoleType role, ChangeEventArgs args)
	{
		if (args.Value is bool isChecked)
		{
			if (isChecked)
			{
				_selectedRoles.Add(role);
			}
			else
			{
				_selectedRoles.Remove(role);
			}
		}
	}

	private void ToggleRoleEdit(RoleType role, ChangeEventArgs args)
	{
		if (args.Value is bool isChecked)
		{
			if (isChecked)
			{
				_roleEditSelection.Add(role);
			}
			else
			{
				_roleEditSelection.Remove(role);
			}
		}
	}

	private async Task CreateUser()
	{
		if (_selectedCompanyId is null)
		{
			_userErrorMessage = "Please select a company.";
			return;
		}

		if (_selectedRoles.Count == 0)
		{
			_userErrorMessage = "Select at least one role.";
			return;
		}

		var createUser = new CreateUser(
			_selectedCompanyId.Value,
			_userFirstName,
			_userMiddleName,
			_userLastName,
			_userMobilePhone,
			[.. _selectedRoles]);

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post("Users", createUser);
			ToastService.ShowSuccess("User created successfully");
			await LoadData(false);
			CloseCreateUserModal();
		}, "Failed to create user: ");
	}

	private async Task CreateDriver()
	{
		if (string.IsNullOrWhiteSpace(_driverFirstName) || string.IsNullOrWhiteSpace(_driverLastName))
		{
			_driverErrorMessage = "First and last name are required.";
			return;
		}

		var createDriver = new CreateDriver(
			_driverPhoto,
			_driverFirstName,
			_driverMiddleName,
			_driverLastName,
			_driverMobilePhone,
			_selectedDriverCompanyId);

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post("Drivers", createDriver);
			ToastService.ShowSuccess("Driver created successfully");
			await LoadData(false);
			CloseCreateDriverModal();
		}, "Failed to create driver: ");
	}

	private async Task CreateDriverSlot()
	{
		if (_slotDriverId is null || _slotCompanyId is null)
		{
			_slotErrorMessage = "Driver and company are required.";
			return;
		}

		AddNewSlot();
		if (_newSlots.Count == 0)
		{
			_slotErrorMessage = "There are now new slots.";
			return;
		}

		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Drivers/slots?id={_slotDriverId}&companyId={_slotCompanyId}", _newSlots);
			ToastService.ShowSuccess("Driver slot created successfully");
			await LoadData(false);
			CloseDriverSlotModal();
		}, "Failed to create driver slot: ");

		_newSlots.Clear();
	}

	private void AddNewSlot()
	{
		if (_slotDriverId is null || _slotCompanyId is null)
		{
			_slotErrorMessage = "Driver and company are required.";
			return;
		}

		if (_slotEndTime <= _slotStartTime)
		{
			_slotErrorMessage = "End time must be after start time.";
			return;
		}

		var now = DateOnly.FromDateTime(DateTime.Now);
		if (_slotDate < now)
		{
			_slotErrorMessage = "Working day cannot be in the past.";
			return;
		}

		var newSlot = new CreateDriverSlot(_slotStartTime, _slotEndTime, _slotDate);
		_newSlots.Add(newSlot);
		SetInitialSlotTimes([]);
	}

	private void RemoveNewSlot(CreateDriverSlot slot)
	{
		_newSlots.Remove(slot);
		SetInitialSlotTimes([]);
	}

	private async Task ToggleUserActive(User user)
	{
		var targetState = !user.UserInfo.IsActive;
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Users/{user.Id}/active?isActive={targetState.ToString().ToLowerInvariant()}");
			ToastService.ShowSuccess($"User {(targetState ? "activated" : "deactivated")} successfully");
			await LoadData(false);
		}, "Failed to update user status: ");
	}

	private async Task ToggleUserVerified(User user)
	{
		var targetState = !user.UserVerified;
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Users/{user.Id}/verified?isVerified={targetState.ToString().ToLowerInvariant()}");
			ToastService.ShowSuccess($"User {(targetState ? "verified" : "unverified")} successfully");
			await LoadData(false);
		}, "Failed to update user verification: ");
	}

	private async Task UpdateRoles()
	{
		if (_rolesTargetUser is null)
		{
			return;
		}

		if (_roleEditSelection.Count == 0)
		{
			_rolesErrorMessage = "Select at least one role.";
			return;
		}

		var roles = _roleEditSelection.Distinct().ToArray();
		var rolesQuery = string.Join("&roles=", roles.Select(role => role.ToString()));
		await LoadingService.Run(ToastService, async () =>
		{
			await ApiClient.Post($"Users/{_rolesTargetUser.Id}/roles?roles={rolesQuery}");
			ToastService.ShowSuccess("User roles updated successfully");
			await LoadData(false);
			CloseRolesModal();
		}, "Failed to update roles: ");
	}

	private void SetInitialSlotTimes(IEnumerable<DriverSlot> slots)
	{
		var now = DateOnly.FromDateTime(DateTime.Now);
		if (slots.Any())
		{
			var initialSlots = slots.Select(s => new CreateDriverSlot(s.StartTime, s.EndTime, s.WorkingDay));
			_newSlots.AddRange(initialSlots);
			var lastSlot = slots.MaxBy(s => s.WorkingDay)!;
			_slotDate = lastSlot.WorkingDay < DateOnly.FromDateTime(DateTime.Now) ? now : lastSlot.WorkingDay.AddDays(1);
			_slotStartTime = lastSlot.StartTime;
			_slotEndTime = lastSlot.EndTime;

		}
		else if (_newSlots.Count > 0)
		{
			var lastNewSlot = _newSlots.MaxBy(s => s.WorkingDay)!;
			_slotDate = lastNewSlot.WorkingDay.AddDays(1);
			_slotStartTime = lastNewSlot.StartTime;
			_slotEndTime = lastNewSlot.EndTime;
		}
		else
		{
			_slotDate = now;
			_slotStartTime = new TimeOnly(8, 0);
			_slotEndTime = new TimeOnly(20, 0);
		}
	}
}
