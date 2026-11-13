using System.Collections.Frozen;
using Microsoft.AspNetCore.Components;

namespace WebWasm.Components;

/// <summary>Icon from the sprite wwwroot/icons.svg. Stroke and colour come from CSS (.icon in app.css, currentColor).</summary>
public partial class Icon : ComponentBase
{
	private static readonly FrozenDictionary<IconName, string> Hrefs = Enum.GetValues<IconName>()
		.ToFrozenDictionary(name => name, name => "icons.svg#i-" + Kebab(name.ToString()));

	[Parameter, EditorRequired] public IconName Name { get; set; }

	/// <summary>16 in text and tables, 20 in the menu and icon buttons (design code, section 5).</summary>
	[Parameter] public int Size { get; set; } = 16;

	[Parameter] public string? Class { get; set; }

	private string Href => Hrefs[Name];

	private static string Kebab(string pascal) =>
		string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}
