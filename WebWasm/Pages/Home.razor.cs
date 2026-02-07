using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Home(CashService cashService)
{
	public CountsInfo? Info => countsInfo;
	private CountsInfo? countsInfo;

	int count = 0;
	protected override async Task OnInitializedAsync()
	{
		countsInfo = await cashService.GetCounts();
		StateHasChanged();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		countsInfo = await cashService.GetCounts();
	}

	private string GetRevenue()
	{
		return Info is null ? string.Empty : $"{Info.Turnover:F2} BYN";
	}

	private ActivityRecord[] GetActivities()
	{
		return Info?.Activities.ToArray() ?? [];
	}

	private static string GetDate(ActivityRecord record)
	{
		var date = record.Date.ToString("MM.dd");
		var start = record.Date.ToString("HH:mm");
		return $"{date} - {start}";
	}
}

public record CountsInfo(int Orders, int Users, int Companies, decimal Turnover, ICollection<ActivityRecord> Activities);

