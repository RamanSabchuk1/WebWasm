using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class PreferenceSwitches
{
	[Inject] private Preferences Preferences { get; set; } = default!;

	/// <summary>Sidebar: short labels (RU/EN, theme icons) without captions.</summary>
	[Parameter] public bool Compact { get; set; }

	private IEnumerable<(ThemeMode Mode, IconName Icon, string Title)> Themes =>
	[
		(ThemeMode.System, IconName.Monitor, T.Shell.ThemeSystem),
		(ThemeMode.Light, IconName.Sun, T.Shell.ThemeLight),
		(ThemeMode.Dark, IconName.Moon, T.Shell.ThemeDark),
	];
}
