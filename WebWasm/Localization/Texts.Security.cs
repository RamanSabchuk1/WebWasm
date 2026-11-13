using WebWasm.Models;

namespace WebWasm.Localization;

public sealed partial class Texts
{
	public SecurityTexts Security => field ??= new(Language);
}

/// <summary>SecurityLevelDialog: data security level of a company or a user.</summary>
public sealed class SecurityTexts(Language language) : TextBase(language)
{
	public string Title => T("Уровень доступа к данным", "Data security level");

	/// <summary>Table column and menu item.</summary>
	public string Short => T("Уровень доступа", "Security level");

	public string Level => T("Уровень", "Level");
	public string WhatItMeans => T("Что это значит", "What it means");

	public string Description(DataSecurityLevel level) => level switch
	{
		DataSecurityLevel.Public => T("Открытые данные, без ограничений.", "Public data, no restrictions."),
		DataSecurityLevel.Internal => T("Внутренние данные компании, например корпоративная почта.", "Internal company data, for example the corporate email."),
		DataSecurityLevel.CompanyOperational => T("Рабочие данные компании, например мобильный телефон и госномер.",
			"Company operational data, for example the mobile phone and the plate number."),
		DataSecurityLevel.Restricted => T("Ограниченный доступ, например адрес, УНП и БИК.", "Restricted access, for example the address, UNP and BIC."),
		DataSecurityLevel.Sensitive => T("Конфиденциальные данные: паспорт и номер банковского счёта.", "Sensitive data: passport fields and the bank account number."),
		DataSecurityLevel.SystemOnly => T("Только для системы, зарезервировано.", "System only, reserved."),
		_ => string.Empty,
	};

	public string Change(string current, string next) => T(
		$"Сейчас уровень «{current}». После сохранения он станет «{next}».",
		$"The current level is «{current}». Saving changes it to «{next}».");

	/// <summary>Whose level the dialog shows.</summary>
	public string CompanyLabel(string name) => T($"Компания «{name}»", $"Company «{name}»");

	public string UserLabel(string name) => T($"Пользователь «{name}»", $"User «{name}»");

	public string Saved => T("Уровень доступа изменён", "Security level changed");
	public string LoadFailed(string reason) => T($"Не удалось загрузить уровень доступа: {reason}", $"Could not load the security level: {reason}");
	public string SaveFailed(string reason) => T($"Не удалось изменить уровень доступа: {reason}", $"Could not change the security level: {reason}");
}
