using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Home(CashService cashService)
{
	private CountsInfo? countsInfo;
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			countsInfo = await cashService.GetCounts();
			StateHasChanged();
		}
	}

	private string GetRevenue()
	{
		return countsInfo is null ? string.Empty : $"{countsInfo.Turnover:F2} BYN";
	}

	private ActivityRecord[] GetActivities()
	{
		return countsInfo?.Activities.ToArray() ?? [];
	}

	private static string GetDate(ActivityRecord record)
	{
		var date = record.Date.ToString("MM.dd");
		var start = record.Date.ToString("HH:mm");
		return $"{date} - {start}";
	}
}

public record CountsInfo(int Orders, int Users, int Companies, decimal Turnover, ICollection<ActivityRecord> Activities);

