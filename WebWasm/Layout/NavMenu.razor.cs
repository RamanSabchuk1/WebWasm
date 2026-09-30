using Microsoft.AspNetCore.Components;
using WebWasm.Services;

namespace WebWasm.Layout;

public partial class NavMenu : ComponentBase
{
	[Inject] private LocalStorageAuthStateProvider AuthProvider { get; set; } = default!;

	[Parameter] public bool IsMobile { get; set; }

	private bool _collapseNavMenu = true;

	private void ToggleNavMenu() => _collapseNavMenu = !_collapseNavMenu;

	private Task Logout() => AuthProvider.MarkUserAsLoggedOut().AsTask();
}
