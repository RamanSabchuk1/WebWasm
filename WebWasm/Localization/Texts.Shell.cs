namespace WebWasm.Localization;

public sealed partial class Texts
{
	public ShellTexts Shell => field ??= new(Language);
	public LoginTexts Login => field ??= new(Language);
	public PushTexts Push => field ??= new(Language);
}

/// <summary>Navigation, layout, preferences, app update banner, «not found».</summary>
public sealed class ShellTexts(Language language) : TextBase(language)
{
	public string AppName => "Kliffort Admin";

	public string GroupMain => T("Основное", "Main");
	public string GroupManagement => T("Управление", "Management");
	public string GroupReference => T("Справочники", "Reference");

	public string Home => T("Главная", "Home");
	public string Orders => T("Заказы", "Orders");
	public string Users => T("Пользователи", "Users");
	public string Companies => T("Компании", "Companies");
	public string Providers => T("Поставщики", "Suppliers");
	public string Vehicles => T("Машины", "Vehicles");
	public string Payments => T("Платежи", "Payments");
	public string EncryptionKeys => T("Ключи шифрования", "Encryption keys");
	public string VehicleCapacities => T("Тоннажи", "Capacities");
	public string PricingSettings => T("Настройки цен", "Pricing settings");
	public string Regions => T("Регионы", "Regions");
	public string Materials => T("Материалы", "Materials");
	public string Devices => T("Устройства", "Devices");
	public string Support => T("Поддержка", "Support");
	public string Logs => T("Журнал", "Logs");
	public string Profile => T("Профиль", "Profile");
	public string More => T("Ещё", "More");
	public string Menu => T("Меню", "Menu");
	public string Logout => T("Выйти", "Log out");

	public string LanguageLabel => T("Язык", "Language");
	public string Russian => "Русский";
	public string English => "English";
	public string ThemeLabel => T("Тема", "Theme");
	public string ThemeSystem => T("Системная", "System");
	public string ThemeLight => T("Светлая", "Light");
	public string ThemeDark => T("Тёмная", "Dark");
	public string Version(string version) => T($"Версия {version}", $"Version {version}");

	public string UpdateAvailable => T("Вышла новая версия админки", "A new version of the admin is available");
	public string UpdateNow => T("Обновить", "Update");
	public string UpdateFailed(string reason) => T($"Не удалось обновить: {reason}", $"Update failed: {reason}");

	public string NotFoundTitle => T("Страница не найдена", "Page not found");
	public string NotFoundHint => T("Такой страницы нет. Проверьте адрес или вернитесь на главную.", "There is no such page. Check the address or go to the home page.");
	public string GoHome => T("На главную", "Go to home page");
}

public sealed class LoginTexts(Language language) : TextBase(language)
{
	public string Title => T("Вход в админку", "Sign in");
	public string Subtitle => T("Для суперадминистраторов Kliffort", "For Kliffort super admins");
	public string Login => T("Логин", "Login");
	public string LoginPlaceholder => T("Телефон или логин", "Phone or login");
	public string Password => T("Пароль", "Password");
	public string SignIn => T("Войти", "Sign in");
	public string SigningIn => T("Вход…", "Signing in…");
	public string Empty => T("Введите логин и пароль.", "Enter the login and the password.");
	public string Failed(string reason) => T($"Не удалось войти: {reason}", $"Sign-in failed: {reason}");
}

public sealed class PushTexts(Language language) : TextBase(language)
{
	public string Notifications => T("Уведомления", "Notifications");
	public string Checking => T("Проверка…", "Checking…");
	public string Subscribing => T("Подключение…", "Connecting…");
	public string On => T("Включены", "On");
	public string Denied => T("Запрещены", "Blocked");
	public string Error => T("Ошибка", "Error");
	public string Unavailable => T("Недоступны", "Unavailable");

	public string Enable => T("Включить push-уведомления", "Turn on push notifications");
	public string Rebind => T("Уведомления включены. Нажмите, чтобы получать их как текущий пользователь",
		"Notifications are on. Tap to receive them as the current user");
	public string FailedTitle => T("Не удалось включить уведомления", "Could not turn on notifications");
	public string NoToken => T("Firebase не выдал токен устройства", "Firebase returned no device token");

	public string InstallRequired => T(
		"На iPhone и iPad уведомления работают только в установленном приложении: «Поделиться» → «На экран „Домой“», затем откройте админку с иконки.",
		"On iPhone and iPad notifications work only in the installed app: Share → Add to Home Screen, then open the admin from its icon.");
	public string DeniedHint => T("Уведомления запрещены в настройках браузера для этого сайта.", "Notifications are blocked for this site in the browser settings.");
	public string UnsupportedHint => T("Этот браузер не поддерживает push-уведомления.", "This browser does not support push notifications.");

	public string History => T("Уведомления", "Notifications");
	public string HistoryEmpty => T("Тут пока пусто", "Nothing here yet");
	public string HistoryEmptyHint => T("Здесь появятся уведомления о заказах. Включите их колокольчиком выше.",
		"Order notifications will appear here. Turn them on with the bell above.");
	public string HistoryLoadFailed(string reason) => T($"Не удалось загрузить уведомления: {reason}", $"Could not load notifications: {reason}");
}
