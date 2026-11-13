namespace WebWasm.Localization;

public sealed partial class Texts
{
	public UsersTexts Users => field ??= new(Language);
}

/// <summary>Users page: list, filters, user and driver forms, roles, company binding, passport, driver slots.</summary>
public sealed class UsersTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Учётные записи, роли, компании и данные водителей", "Accounts, roles, companies and driver data");
	public string AddUser => T("Добавить пользователя", "Add user");
	public string AddDriver => T("Добавить водителя", "Add driver");
	public string Unknown => T("Без имени", "No name");

	public string TotalUsers => T("Всего пользователей", "Total users");
	public string ActiveUsers => T("Активные", "Active");
	public string SuperAdmins => T("Суперадмины", "Super admins");
	public string Drivers => T("Водители", "Drivers");

	public string SearchPlaceholder => T("Поиск по имени, телефону и логину", "Search by name, phone and login");
	public string Filters => T("Фильтры", "Filters");
	public string ActiveFilters(int count) => T($"Включено фильтров: {count}", $"{count} filter(s) on");
	public string Kind => T("Тип пользователя", "User kind");
	public string NotDrivers => T("Не водители", "Not drivers");
	public string Roles => T("Роли", "Roles");
	public string SelectRole => T("Выберите роль", "Select a role");
	public string SelectCompany => T("Выберите компанию", "Select a company");
	public string Activity => T("Активность", "Activity");
	public string ActiveMany => T("Активные", "Active");
	public string InactiveMany => T("Неактивные", "Inactive");
	public string Verification => T("Проверка", "Verification");
	public string VerifiedMany => T("Проверенные", "Verified");
	public string UnverifiedMany => T("Непроверенные", "Unverified");
	public string Customers => T("Покупатели", "Customers");
	public string WithoutRolesOnly => T("Только пользователи без ролей", "Only users without roles");
	public string RemoveFilter(string name) => T($"Убрать фильтр «{name}»", $"Remove the «{name}» filter");

	public string Login => T("Логин", "Login");
	public string Verified => T("Проверен", "Verified");
	public string Unverified => T("Не проверен", "Not verified");
	public string HasDriver => T("Есть запись водителя", "Has a driver record");
	public string Empty => T("Пользователей пока нет", "No users yet");
	public string EmptyHint => T("Добавьте первого кнопкой «Добавить пользователя».", "Add the first one with «Add user».");

	public string UserInfo => T("Пользователь", "User");
	public string FullName => T("ФИО", "Full name");
	public string Passport => T("Паспорт", "Passport");
	public string PassportNumber => T("Номер", "Number");
	public string IdentificationNumber => T("Идентификационный номер", "Identification number");
	public string IssuedBy => T("Кем выдан", "Issued by");
	public string IssuedDate => T("Дата выдачи", "Issue date");
	public string Driver => T("Водитель", "Driver");
	public string NoCompany => T("Без компании", "No company");
	public string Slots => T("Доступные слоты", "Available slots");
	public string NoSlotsWithoutCompany => T("Водитель без компании, слоты недоступны", "The driver has no company, so no slots");
	public string NoSlots => T("Слоты не заданы", "No slots set");

	public string Activate => T("Активировать", "Activate");
	public string Deactivate => T("Деактивировать", "Deactivate");
	public string Verify => T("Отметить проверенным", "Mark as verified");
	public string Unverify => T("Снять проверку", "Remove verification");
	public string EditRoles => T("Изменить роли", "Edit roles");
	public string PassportData => T("Паспортные данные", "Passport data");
	public string ResetPassword => T("Сбросить пароль", "Reset password");
	public string AddSlots => T("Слоты водителя", "Driver slots");
	public string CreateDriver => T("Создать водителя", "Create driver");
	public string DeleteDriver => T("Удалить водителя", "Delete driver");
	public string DeleteUser => T("Удалить пользователя", "Delete user");
	public string AssignCompany => T("Привязать к компании", "Assign to a company");
	public string UnassignCompany => T("Отвязать от компании", "Unlink from the company");

	public string NewUser => T("Новый пользователь", "New user");
	public string NewDriver => T("Новый водитель", "New driver");
	public string FirstName => T("Имя", "First name");
	public string MiddleName => T("Отчество", "Middle name");
	public string LastName => T("Фамилия", "Last name");
	public string MobilePhone => T("Мобильный телефон", "Mobile phone");
	public string PhotoUrl => T("Ссылка на фото", "Photo URL");
	public string PhotoUrlPlaceholder => T("https://… (необязательно)", "https://… (optional)");
	public string SelectCompanyError => T("Выберите компанию.", "Select a company.");
	public string SelectRoleError => T("Выберите хотя бы одну роль.", "Select at least one role.");
	public string NameRequired => T("Укажите имя и фамилию.", "Enter the first and the last name.");

	public string SlotsHint => T(
		"Сохранение заменит все слоты водителя этим списком, вместе со слотом из полей выше.",
		"Saving replaces all the driver's slots with this list, together with the slot in the fields above.");
	public string Start => T("Начало", "Start");
	public string End => T("Конец", "End");
	public string AddSlot => T("Добавить слот", "Add slot");
	public string RemoveSlot(string slot) => T($"Убрать слот {slot}", $"Remove slot {slot}");
	public string MaxSlots => T("Не больше 15 слотов.", "15 slots at most.");
	public string DriverAndCompanyRequired => T("Нужны водитель и компания.", "A driver and a company are required.");
	public string NoNewSlots => T("Добавьте хотя бы один слот.", "Add at least one slot.");
	public string EndAfterStart => T("Конец должен быть позже начала.", "The end must be after the start.");
	public string DayInPast => T("Дата не может быть в прошлом.", "The date cannot be in the past.");

	public string Assign => T("Привязать", "Assign");
	public string PassportTitle(string name) => T($"Паспортные данные: {name}", $"Passport data: {name}");
	public string FillOneField => T("Заполните хотя бы одно поле.", "Fill in at least one field.");

	public string UnassignTitle => T("Отвязать от компании?", "Unlink from the company?");
	public string UnassignMessage(string name) => T($"Пользователь «{name}» будет отвязан от своей компании.", $"«{name}» will be unlinked from their company.");
	public string Unassign => T("Отвязать", "Unlink");
	public string DeleteUserTitle => T("Удалить пользователя?", "Delete the user?");
	public string DeleteUserMessage(string name, string login) => T(
		$"Пользователь «{name}» (логин {login}) будет удалён вместе с записью водителя и его машинами. Быстро отменить это нельзя.",
		$"«{name}» (login {login}) will be deleted together with the driver record and its vehicles. This cannot be easily undone.");
	public string ResetPasswordTitle => T("Сбросить пароль?", "Reset the password?");
	public string ResetPasswordMessage(string name, string login) => T(
		$"Пользователь «{name}» (логин {login}) получит новый пароль по SMS. Старый пароль сразу перестанет работать.",
		$"«{name}» (login {login}) will get a new password by SMS. The old password stops working at once.");
	public string DeleteDriverTitle => T("Удалить водителя?", "Delete the driver?");
	public string DeleteDriverMessage(string name) => T(
		$"Запись водителя «{name}» будет удалена вместе с его машинами. Учётная запись пользователя останется.",
		$"The driver record of «{name}» will be deleted together with its vehicles. The user account stays.");
	public string CreateDriverTitle => T("Создать водителя?", "Create a driver?");
	public string CreateDriverMessage(string name, string login) => T(
		$"Для пользователя «{name}» (логин {login}) будет создана запись водителя.",
		$"A driver record will be created for «{name}» (login {login}).");

	public string UserCreated => T("Пользователь добавлен", "User added");
	public string DriverCreated => T("Водитель создан", "Driver created");
	public string UserDeleted => T("Пользователь удалён", "User deleted");
	public string DriverDeleted => T("Водитель удалён", "Driver deleted");
	public string Activated => T("Пользователь активирован", "User activated");
	public string Deactivated => T("Пользователь деактивирован", "User deactivated");
	public string VerifiedToast => T("Пользователь отмечен проверенным", "User marked as verified");
	public string UnverifiedToast => T("Проверка пользователя снята", "User verification removed");
	public string RolesSaved => T("Роли сохранены", "Roles saved");
	public string Assigned => T("Пользователь привязан к компании", "User assigned to the company");
	public string Unassigned => T("Пользователь отвязан от компании", "User unlinked from the company");
	public string PasswordReset => T("Пароль сброшен, новый отправлен пользователю по SMS", "Password reset, the new one was sent to the user by SMS");
	public string PassportSaved => T("Паспортные данные сохранены", "Passport data saved");
	public string SlotsSaved => T("Слоты водителя сохранены", "Driver slots saved");

	public string CreateUserFailed(string reason) => T($"Не удалось добавить пользователя: {reason}", $"Could not add the user: {reason}");
	public string CreateDriverFailed(string reason) => T($"Не удалось создать водителя: {reason}", $"Could not create the driver: {reason}");
	public string DeleteUserFailed(string reason) => T($"Не удалось удалить пользователя: {reason}", $"Could not delete the user: {reason}");
	public string DeleteDriverFailed(string reason) => T($"Не удалось удалить водителя: {reason}", $"Could not delete the driver: {reason}");
	public string StatusFailed(string reason) => T($"Не удалось изменить активность пользователя: {reason}", $"Could not change the user's activity: {reason}");
	public string VerificationFailed(string reason) => T($"Не удалось изменить проверку пользователя: {reason}", $"Could not change the user's verification: {reason}");
	public string RolesFailed(string reason) => T($"Не удалось сохранить роли: {reason}", $"Could not save the roles: {reason}");
	public string AssignFailed(string reason) => T($"Не удалось привязать к компании: {reason}", $"Could not assign to the company: {reason}");
	public string UnassignFailed(string reason) => T($"Не удалось отвязать от компании: {reason}", $"Could not unlink from the company: {reason}");
	public string ResetPasswordFailed(string reason) => T($"Не удалось сбросить пароль: {reason}", $"Could not reset the password: {reason}");
	public string PassportFailed(string reason) => T($"Не удалось сохранить паспортные данные: {reason}", $"Could not save the passport data: {reason}");
	public string SlotsFailed(string reason) => T($"Не удалось сохранить слоты водителя: {reason}", $"Could not save the driver slots: {reason}");
}
