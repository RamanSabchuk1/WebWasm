using Microsoft.AspNetCore.Components;

namespace WebWasm.Localization;

/// <summary>Base of components that show text: <see cref="T"/> is the current language, cascaded from <see cref="Services.Preferences"/>.</summary>
public abstract class LocalizedComponent : ComponentBase
{
	[CascadingParameter] protected Texts T { get; set; } = Texts.Ru;
}
