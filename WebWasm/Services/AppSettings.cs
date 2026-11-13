using System.Reflection;

namespace WebWasm.Services;

/// <summary>
/// What differs between the dev and the prod admin: <c>wwwroot/appsettings.json</c> (dev) and
/// <c>appsettings.Prod.json</c> (prod overrides). <see cref="EnvironmentLabel"/> is empty in prod.
/// </summary>
public sealed record AppSettings(string Name, string? EnvironmentLabel, Uri ApiBaseAddress)
{
	/// <summary>Version from the project file (<c>&lt;Version&gt;</c>), without the commit hash the SDK appends.</summary>
	public static string Version { get; } = typeof(AppSettings).Assembly
		.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";

	/// <summary>Browser tab title: «Orders · Kliffort Admin DEV».</summary>
	public string Title(string page) => EnvironmentLabel is null ? $"{page} · {Name}" : $"{page} · {Name} {EnvironmentLabel}";

	public static AppSettings From(IConfiguration configuration)
	{
		var label = configuration["App:EnvironmentLabel"];
		return new AppSettings(
			Required(configuration, "App:Name"),
			string.IsNullOrWhiteSpace(label) ? null : label,
			new Uri(Required(configuration, "Api:BaseAddress")));
	}

	private static string Required(IConfiguration configuration, string key) =>
		configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"'{key}' is missing in appsettings.json.");
}
