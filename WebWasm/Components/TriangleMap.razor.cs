using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebWasm.Components;

public partial class TriangleMap : ComponentBase
{
	[Inject] protected IJSRuntime JS { get; set; } = default!;
}
