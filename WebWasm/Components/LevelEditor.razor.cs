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
	private Dictionary<uint, PriceInfo>? _priceInfo = null;
	private List<Location> _points = [];
	private bool _showDrawer = false;
	private string _errorMessage = string.Empty;
	private bool _isInitialized = false;

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
		// Only reset/initialize when dialog OPENS (IsOpen changes from false to true)
		if (IsOpen && !_isInitialized)
		{
			_isInitialized = true;
			InitializeForm();
		}

		// When dialog CLOSES (IsOpen changes from true to false)
		if (!IsOpen && _isInitialized)
		{
			_isInitialized = false;
		}
	}

	private void InitializeForm()
	{
		if (IsEditMode && EditingLevel is not null)
		{
			_levelType = EditingLevel.Type;
			_algorithm = EditingLevel.CalculationAlgorithm;
			
			// Get price info from dictionary - IMPORTANT: Create new dictionary instance
			_priceInfo = null; // Reset first
			if (EditingLevel.Info?.Info?.Count > 0)
			{
				_priceInfo = new Dictionary<uint, PriceInfo>(EditingLevel.Info.Info);
			}
			else
			{
				_priceInfo = new Dictionary<uint, PriceInfo>();
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
			// Reset for new level
			_levelType = LevelType.Neighborhood;
			_algorithm = 1;
			_priceInfo = null;
			_points.Clear();
			_showDrawer = false;
		}
		
		// Force re-render to ensure child components get updated parameters
		StateHasChanged();
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

		var priceInfo = _priceEditor?.GetPrices();
		if (priceInfo is null || priceInfo.Count == 0)
		{
			_errorMessage = "Invalid price information.";
			return;
		}

		try
		{
			var createLevel = new CreateLevel(
				_levelType,
				_algorithm,
				_points,
				priceInfo
			);

			await OnSubmit.InvokeAsync(createLevel);
			// Close modal immediately after submission, regardless of result
			await CloseEditor();
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error: {ex.Message}";
		}
	}

	private async Task CloseEditor()
	{
		// Mark as uninitialized to trigger reset on next open
		_isInitialized = false;
		await OnClose.InvokeAsync();
	}

	private string GetPriceEditorKey()
	{
		// Generate unique key based on whether we're editing or creating
		// This forces component recreation when switching between edit/create
		if (IsEditMode && EditingLevel != null)
		{
			// Include hash of price info to force recreation when prices change
			var priceHash = _priceInfo?.Count ?? 0;
			return $"edit-{EditingLevel.Id}-{priceHash}-{_isInitialized}";
		}
		return $"create-new-{_isInitialized}";
	}
}
