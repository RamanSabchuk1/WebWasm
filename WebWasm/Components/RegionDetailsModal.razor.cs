using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class RegionDetailsModal : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public Region? Region { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<Region> OnAddLevel { get; set; }
	[Parameter] public EventCallback<(Region, Guid)> OnDeleteLevel { get; set; }
	[Parameter] public EventCallback<(Region, Level)> OnEditLevel { get; set; }

	private async Task CloseModal()
	{
		await OnClose.InvokeAsync();
	}

	private static string AlgorithmTranslation(byte algorithmVersion)
	{
		return algorithmVersion switch
		{
			1 => "1 (price per KM)",
			2 => "2 (price per Hour)",
			_ => "⛔ None"
		};
	}
}
