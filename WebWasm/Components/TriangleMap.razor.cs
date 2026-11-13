using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class TriangleMap : LeafletMapBase
{
	[Parameter] public ICollection<Triangle>? Triangles { get; set; }
	[Parameter] public LevelType LevelType { get; set; }
	[Parameter] public string MapHeight { get; set; } = "400px";
	[Parameter] public bool ShowTitle { get; set; } = false;

	protected override string ModulePath => "./js/triangle-map.js";

	protected override bool ShouldCreateMap => Triangles?.Count > 0;

	protected override (string Function, object?[] Args) MapInit()
	{
		var triangleData = Triangles!.Select(t => new
		{
			point1 = new { lat = t.Point1.Latitude, lng = t.Point1.Longitude },
			point2 = new { lat = t.Point2.Latitude, lng = t.Point2.Longitude },
			point3 = new { lat = t.Point3.Latitude, lng = t.Point3.Longitude }
		}).ToList();

		// Design token, the module reads its value: the colour follows the theme.
		// Orange is for actions only (design code, section 2): city levels take the warning tone.
		var colorVar = LevelType == LevelType.Neighborhood ? "--info" : "--warning";

		return ("initMap", [triangleData, colorVar, T.Maps.Triangle]);
	}
}
