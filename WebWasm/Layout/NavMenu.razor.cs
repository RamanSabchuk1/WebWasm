using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using WebWasm.Components;
using WebWasm.Localization;
using WebWasm.Services;

namespace WebWasm.Layout;

public partial class NavMenu
{
	[Inject] private LocalStorageAuthStateProvider AuthProvider { get; set; } = default!;
	[Inject] private AppSettings Settings { get; set; } = default!;

	[Parameter] public bool IsMobile { get; set; }

	private bool _moreOpen;

	private sealed record NavItem(string Href, IconName Icon, Func<ShellTexts, string> Title, NavLinkMatch Match = NavLinkMatch.Prefix);

	private sealed record NavGroup(string Key, Func<ShellTexts, string> Title, NavItem[] Items);

	private static readonly NavItem Home = new("", IconName.Home, t => t.Home, NavLinkMatch.All);
	private static readonly NavItem Orders = new("orders", IconName.Orders, t => t.Orders);
	private static readonly NavItem Users = new("users", IconName.Users, t => t.Users);
	private static readonly NavItem Companies = new("companies", IconName.Companies, t => t.Companies);

	private static readonly NavGroup[] Groups =
	[
		new("main", t => t.GroupMain, [Home, Orders, Users, Companies]),
		new("management", t => t.GroupManagement,
		[
			new("providers", IconName.Providers, t => t.Providers),
			new("vehicles", IconName.Vehicles, t => t.Vehicles),
			new("payments", IconName.Payments, t => t.Payments),
			new("pricing-settings", IconName.Pricing, t => t.PricingSettings),
			new("vehicle-capacities", IconName.Capacities, t => t.VehicleCapacities),
			new("encryption-keys", IconName.Keys, t => t.EncryptionKeys),
		]),
		new("reference", t => t.GroupReference,
		[
			new("regions", IconName.Regions, t => t.Regions),
			new("materials", IconName.Materials, t => t.Materials),
			new("devices", IconName.Devices, t => t.Devices),
			new("supports", IconName.Support, t => t.Support),
		]),
	];

	/// <summary>Phone: four sections on the bottom bar, the rest in «More».</summary>
	private static readonly NavItem[] Tabs = [Home, Orders, Users, Companies];

	private static readonly NavItem[] MoreItems =
	[
		.. Groups.SelectMany(g => g.Items).Except(Tabs),
		new("myuserinfo", IconName.User, t => t.Profile),
		new("logs", IconName.Logs, t => t.Logs),
	];

	private void ToggleMore() => _moreOpen = !_moreOpen;

	private Task Logout() => AuthProvider.MarkUserAsLoggedOut().AsTask();
}
