using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class LevelEditor : ComponentBase
{
	[Parameter] public bool IsOpen { get; set; }
	[Parameter] public Region? Region { get; set; }
	[Parameter] public Level? EditingLevel { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
	[Parameter] public EventCallback<CreateLevel> OnSubmit { get; set; }

	private PriceInfoEditor? _priceEditor;
	private LevelType _levelType = LevelType.Neighborhood;
	private byte _algorithm = 1;
	private PriceInfo? _priceInfo = null;
	private List<Location> _points = [];
	private bool _showDrawer = false;
	private string _errorMessage = string.Empty;

	private bool IsEditMode => EditingLevel is not null;

	private bool CanSubmit
	{
		get
		{
			return _points.Count >= 3 && _priceEditor?.IsValid == true;
		}
	}

	protected override void OnParametersSet()
	{
		if (IsOpen)
		{
			if (IsEditMode && EditingLevel is not null)
			{
				_levelType = EditingLevel.Type;
				_algorithm = EditingLevel.CalculationAlgorithm;
				
				// Get first price info from dictionary (or use default)
				if (EditingLevel.Info?.Info?.Count > 0)
				{
					_priceInfo = EditingLevel.Info.Info.Values.FirstOrDefault();
				}
				
				// Extract points from triangles
				if (EditingLevel.Triangles?.Count > 0)
				{
					var pointSet = new HashSet<string>();
					_points.Clear();
					
					foreach (var triangle in EditingLevel.Triangles)
					{
						AddPointIfUnique(triangle.Point1, pointSet);
						AddPointIfUnique(triangle.Point2, pointSet);
						AddPointIfUnique(triangle.Point3, pointSet);
					}
				}
			}
			else
			{
				ResetForm();
			}
			_errorMessage = string.Empty;
		}
	}

	private void AddPointIfUnique(Location point, HashSet<string> pointSet)
	{
		var key = $"{point.Latitude:F6}_{point.Longitude:F6}";
		if (!pointSet.Contains(key))
		{
			pointSet.Add(key);
			_points.Add(point);
		}
	}

	private void OpenDrawer()
	{
		_showDrawer = true;
	}

	private void CancelDrawer()
	{
		_showDrawer = false;
	}

	private async Task HandleDrawerConfirm(ICollection<Location> drawnPoints)
	{
		_points = drawnPoints.ToList();
		_showDrawer = false;
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task HandleSubmit()
	{
		if (!CanSubmit)
		{
			_errorMessage = "Please complete all fields and draw a valid polygon (minimum 3 points).";
			return;
		}

		var priceInfo = _priceEditor?.GetPrice();
		if (priceInfo is null)
		{
			_errorMessage = "Invalid price information.";
			return;
		}

		var createLevel = new CreateLevel(
			_levelType,
			_algorithm,
			_points,
			new Dictionary<uint, PriceInfo> { { 0, priceInfo.Value } } // Using 0 as default key
		);

		await OnSubmit.InvokeAsync(createLevel);
		ResetForm();
	}

	private async Task CloseEditor()
	{
		ResetForm();
		await OnClose.InvokeAsync();
	}

	private void ResetForm()
	{
		_levelType = LevelType.Neighborhood;
		_algorithm = 1;
		_priceInfo = null;
		_points.Clear();
		_showDrawer = false;
		_errorMessage = string.Empty;
	}
}
