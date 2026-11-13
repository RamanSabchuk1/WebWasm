namespace WebWasm.Localization;

public sealed partial class Texts
{
	public KeysTexts Keys => field ??= new(Language);
}

/// <summary>EncryptionKeys page and RotateKeyDialog. Purpose names come from <see cref="EnumTexts.EncryptionPurpose"/>.</summary>
public sealed class KeysTexts(Language language) : TextBase(language)
{
	public string Subtitle => T("Сведения о ключах шифрования защищённых данных. Сами ключи здесь не показываются.",
		"Metadata of the secure-data encryption keys. The keys themselves are never shown.");

	public string BackfillTitle => T("Перенос данных в шифрование", "Secure-data backfill");
	public string BackfillDescription => T(
		"Переносит открытые данные существующих пользователей, компаний и машин в зашифрованные поля. Повторный запуск безопасен: уже перенесённые поля пропускаются. Блок временный, его уберут вместе со старой логикой переноса.",
		"Moves plaintext data of existing users, companies and vehicles into encrypted fields. Running it again is safe: fields already moved are skipped. This block is temporary and will be removed together with the old backfill logic.");
	public string RunBackfill => T("Запустить перенос", "Run backfill");
	public string BackfillRunning => T("Перенос идёт…", "Running…");
	public string CompaniesInfo => T("Компании: реквизиты", "Companies: details");
	public string CompaniesRoot => T("Компании: скидка и координаты", "Companies: rebate and location");
	public string BackfillDone(int users, int companies, int companyRoots, int vehicles) => T(
		$"Перенос завершён. Пользователи: {users}, компании (реквизиты): {companies}, компании (скидка и координаты): {companyRoots}, машины: {vehicles}.",
		$"Backfill complete: {users} users, {companies} companies (details), {companyRoots} companies (rebate and location), {vehicles} vehicles.");
	public string BackfillFailed(string reason) => T($"Не удалось перенести данные: {reason}", $"Backfill failed: {reason}");

	public string KeysTitle => T("Ключи", "Keys");
	public string Purpose => T("Назначение", "Purpose");
	public string Version => T("Версия", "Version");
	public string Retired => T("Выведен из работы", "Retired");
	public string FullyMigrated => T("Данные перенесены", "Fully migrated");
	public string Migrating => T("Идёт перенос", "Migrating");
	public string ActiveVersion(int version) => T($"Активная версия {version}", $"Active version {version}");
	public string NoActiveKey => T("Нет активного ключа", "No active key");
	public string CreateKey => T("Создать ключ", "Create key");
	public string RotateKey => T("Сменить ключ", "Rotate key");
	public string BlindIndexRotateHint => T(
		"Ключ слепого индекса сменить нельзя: перестанет работать поиск по уникальным значениям, например проверка, что телефон ещё не занят.",
		"The blind index key cannot be rotated: unique lookups would break, for example the check that a phone number is not taken yet.");
	public string Empty => T("Ключей пока нет", "No keys yet");
	public string EmptyHint => T("Создайте первый ключ для нужного назначения кнопкой «Создать ключ».", "Create the first key for a purpose with «Create key».");

	public string Created(string purpose) => T($"Ключ «{purpose}» создан", $"«{purpose}» key created");
	public string CreateFailed(string purpose, string reason) => T($"Не удалось создать ключ «{purpose}»: {reason}", $"Could not create the «{purpose}» key: {reason}");
	public string Rotated(string purpose) => T($"Ключ «{purpose}» сменён. Старые данные перешифруются новым ключом постепенно, в фоне.",
		$"«{purpose}» key rotated. Existing data will be re-encrypted gradually in the background.");
	public string RotateFailed(string purpose, string reason) => T($"Не удалось сменить ключ «{purpose}»: {reason}", $"Could not rotate the «{purpose}» key: {reason}");
	public string LoadFailed(string reason) => T($"Не удалось загрузить ключи шифрования: {reason}", $"Could not load the encryption keys: {reason}");

	public string RotateTitle => T("Сменить ключ шифрования?", "Rotate the encryption key?");
	public string RotateMessage(string purpose) => T(
		$"Появится новая активная версия ключа «{purpose}». Новые записи сразу шифруются ею, старые данные переходят на неё постепенно, в фоне.",
		$"A new active version of the «{purpose}» key will be created. New writes use it at once, existing data moves to it gradually in the background.");
	public string Force => T("Сменить, даже если с прошлой смены прошло слишком мало времени", "Rotate even if the cooldown period has not passed yet");
	public string ForceWarning => T(
		"Так вы обойдёте минимальный интервал между сменами ключа. Включайте, только если понимаете, как это скажется на работе системы.",
		"This bypasses the minimum interval between rotations. Use it only if you understand the operational impact.");
}
