using Microsoft.AspNetCore.Components;
using WebWasm.Localization;
using WebWasm.Services;

namespace WebWasm.Layout;

public partial class MainLayout
{
	[Inject] private AppSettings Settings { get; set; } = default!;

	// A layout inherits LayoutComponentBase, so it takes the texts itself instead of LocalizedComponent.
	[CascadingParameter] private Texts T { get; set; } = Texts.Ru;
}
