using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Components;

public partial class AppTitle
{
	[Inject] private AppSettings Settings { get; set; } = default!;

	[Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
}
