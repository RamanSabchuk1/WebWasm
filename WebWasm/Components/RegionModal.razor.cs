using Microsoft.AspNetCore.Components;
using WebWasm.Models;

namespace WebWasm.Components;

public partial class RegionModal : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public Region? EditingRegion { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<UpdateRegion> OnSubmit { get; set; }

    private string _name = string.Empty;
    private string _errorMessage = string.Empty;
    private List<Level> _existingLevels = [];

    private bool IsEditMode => EditingRegion is not null;
    private bool IsValid => !string.IsNullOrWhiteSpace(_name);

    protected override void OnParametersSet()
    {
        if (IsOpen)
        {
            if (IsEditMode && EditingRegion is not null)
            {
                _name = EditingRegion.Name;
                _existingLevels = EditingRegion.Levels?.ToList() ?? [];
            }
            else
            {
                ResetForm();
            }

            _errorMessage = string.Empty;
        }
    }

    private async Task HandleSubmit()
    {
        if (!IsValid)
        {
            _errorMessage = "Please enter a region name.";
            return;
        }

        var updateRegion = new UpdateRegion(_name.Trim(), _existingLevels.Select(l => ConvertLevelToCreateLevel(l)).ToList());
        await OnSubmit.InvokeAsync(updateRegion);
        ResetForm();
    }

    private CreateLevel ConvertLevelToCreateLevel(Level level)
    {
        var points = level.Triangles?.SelectMany(t => new[] { t.Point1, t.Point2, t.Point3 }).Distinct().ToList() ?? [];
        var priceInfo = level.Info?.Info ?? [];

        return new CreateLevel(level.Type, level.CalculationAlgorithm, points, priceInfo);
    }

    private async Task CloseModal()
    {
        ResetForm();
        await OnClose.InvokeAsync();
    }

    private void ResetForm()
    {
        _name = string.Empty;
        _errorMessage = string.Empty;
        _existingLevels = [];
    }
}
