using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using WebWasm.Localization;

namespace WebWasm.Services;

public enum ThemeMode
{
	System,
	Light,
	Dark,
}

/// <summary>
/// Interface language and theme. The values are chosen and applied before the first paint by the script in index.html
/// (window.appPreferences: saved choice, otherwise browser language and system theme); this service mirrors them for .NET,
/// saves the user's choice and cascades <see cref="Texts"/> to all components.
/// </summary>
public sealed class Preferences(IJSRuntime js)
{
	public CascadingValueSource<Texts> TextsSource { get; } = new(Texts.Ru, isFixed: false);

	public Texts Texts { get; private set; } = Texts.Ru;

	public ThemeMode Theme { get; private set; }

	/// <summary>Called once in Program before the app starts rendering.</summary>
	public async Task Initialize()
	{
		try
		{
			var language = await js.InvokeAsync<string>("appPreferences.language");
			var theme = await js.InvokeAsync<string>("appPreferences.theme");
			Texts = Texts.For(language == "en" ? Language.En : Language.Ru);
			await TextsSource.NotifyChangedAsync(Texts);
			Theme = Enum.TryParse<ThemeMode>(theme, ignoreCase: true, out var mode) ? mode : ThemeMode.System;
		}
		catch (JSException ex)
		{
			// The UI still works in Russian with the system theme.
			Console.WriteLine($"Preferences are not available: {ex.Message}");
		}
	}

	public async Task SetLanguage(Language language)
	{
		Texts = Texts.For(language);
		await TextsSource.NotifyChangedAsync(Texts);
		await Save("appPreferences.setLanguage", Texts.Code);
	}

	public async Task SetTheme(ThemeMode theme)
	{
		Theme = theme;
		await Save("appPreferences.setTheme", theme.ToString().ToLowerInvariant());
	}

	private async Task Save(string function, string value)
	{
		try
		{
			await js.InvokeVoidAsync(function, value);
		}
		catch (JSException ex)
		{
			Console.WriteLine($"{function} failed: {ex.Message}");
		}
	}
}
