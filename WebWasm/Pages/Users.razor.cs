using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Users : ComponentBase
{
	[Inject] private CashService CashService { get; set; } = default!;
	[Inject] private ApiClient ApiClient { get; set; } = default!;
	[Inject] private ToastService ToastService { get; set; } = default!;
	[Inject] private LoadingService LoadingService { get; set; } = default!;

	private User[]? _users;
	private Driver[] _drivers = [];
	private Company[] _companies = [];
	private DriverSlot[] _driverSlots = [];
	private Dictionary<Guid, Driver> _driverByUserInfoId = [];
	private Dictionary<Guid, List<DriverSlot>> _slotsByDriverId = [];
	private readonly HashSet<Guid> _expandedUsers = [];
	private readonly PaginationState _pagination = new() { ItemsPerPage = 10 };
	private bool _isInitialized;

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

	private static readonly RoleType[] _roleOptions = Enum.GetValues<RoleType>();

	protected override async Task OnInitializedAsync()
	{
		if (_isInitialized)
		{
			return;
		}

		_isInitialized = true;
		await LoadData(true);
	}

	private async Task LoadData(bool useCash)
	{
		_users = await CashService.GetData<User>(useCash);
		_companies = await CashService.GetData<Company>(useCash);
		_drivers = await CashService.GetData<Driver>(useCash);
		_driverSlots = await CashService.GetData<DriverSlot>(useCash);
		BuildLookups();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await Task.Delay(100);
			await _pagination.SetCurrentPageIndexAsync(0);
			StateHasChanged();
		}
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
	private int ActiveUsers => _users?.Count(user => user.IsActive) ?? 0;
	private int SuperAdminUsers => _users?.Count(user => user.Roles.Contains(RoleType.SuperAdmin)) ?? 0;
	private int DriverCount => _drivers.Length;

	private Driver? GetDriver(User user)	
		=> _driverByUserInfoId.TryGetValue(user.UserInfo.Id, out var driver) ? driver : null;

	private static Company? GetCompany(User user, Driver? driver)
		=> user.UserInfo.Company ?? driver?.UserInfo?.Company;

	private IEnumerable<DriverSlot> GetDriverSlots(Guid driverId)
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

	private bool IsExpanded(Guid userId) => _expandedUsers.Contains(userId);

	private void ToggleExpanded(Guid userId)
	{
		if (!_expandedUsers.Add(userId))
		{
			_expandedUsers.Remove(userId);
		}
	}

	private static string FormatSlot(DriverSlot slot)
	{
		var date = slot.WorkingDay.ToString("yyyy-MM-dd");
		var start = slot.StartTime.ToString("HH:mm");
		var end = slot.EndTime.ToString("HH:mm");
		return $"{date} {start} - {end}";
	}

	private static string FormatSlot(CreateDriverSlot slot)
	{
		var date = slot.WorkingDay.ToString("yyyy-MM-dd");
		var start = slot.StartTime.ToString("HH:mm");
		var end = slot.EndTime.ToString("HH:mm");
		return $"{date} {start} - {end}";
	}

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
		_newSlots.Clear();
	}

	private void CloseDriverSlotModal()
	{
		_showDriverSlotModal = false;
		_slotErrorMessage = string.Empty;
		_slotDriverId = null;
		_slotCompanyId = null;
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

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post("Users", createUser);
				ToastService.ShowSuccess("User created successfully");
				await LoadData(false);
				CloseCreateUserModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create user: {ex.Message}");
			}
		});
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

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post("Drivers", createDriver);
				ToastService.ShowSuccess("Driver created successfully");
				await LoadData(false);
				CloseCreateDriverModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create driver: {ex.Message}");
			}
		});
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

		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"/Drivers/slots?id={_slotDriverId}&companyId={_slotCompanyId}", _newSlots);
				ToastService.ShowSuccess("Driver slot created successfully");
				await LoadData(false);
				CloseDriverSlotModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to create driver slot: {ex.Message}");
			}
		});

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

	private async Task ToggleUserActive(User user)
	{
		var targetState = !user.IsActive;
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Users/{user.Id}/active?isActive={targetState.ToString().ToLowerInvariant()}");
				ToastService.ShowSuccess($"User {(targetState ? "activated" : "deactivated")} successfully");
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to update user status: {ex.Message}");
			}
		});
	}

	private async Task ToggleUserVerified(User user)
	{
		var targetState = !user.UserVerified;
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Users/{user.Id}/verified?isVerified={targetState.ToString().ToLowerInvariant()}");
				ToastService.ShowSuccess($"User {(targetState ? "verified" : "unverified")} successfully");
				await LoadData(false);
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to update user verification: {ex.Message}");
			}
		});
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
		await LoadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				await ApiClient.Post($"Users/{_rolesTargetUser.Id}/roles?roles={rolesQuery}");
				ToastService.ShowSuccess("User roles updated successfully");
				await LoadData(false);
				CloseRolesModal();
			}
			catch (Exception ex)
			{
				ToastService.ShowError($"Failed to update roles: {ex.Message}");
			}
		});
	}

	private void SetInitialSlotTimes(IEnumerable<DriverSlot> slots)
	{
		var now = DateOnly.FromDateTime(DateTime.Now);
		if (slots.Any())
		{
			var lastSlot = slots.MaxBy(s => s.WorkingDay)!;
			_slotDate = lastSlot.WorkingDay < DateOnly.FromDateTime(DateTime.Now) ? now : lastSlot.WorkingDay.AddDays(1);
			_slotStartTime = lastSlot.StartTime;
			_slotEndTime = lastSlot.EndTime;

		}
		else if(_newSlots.Count > 0)
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
