using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Home(CashService cashService, ApiClient api)
{
	private int _ordersTodayCount;
	private int _usersCount;
	private int _companiesCount;
	private decimal _turnover;
	private ActivityRecord[] _activityRecords = [];

	protected override async Task OnInitializedAsync()
	{
		await LoadData();
	}

	private async ValueTask LoadData()
	{
		_activityRecords = await cashService.GetData<ActivityRecord>();
		_ordersTodayCount = await api.Get<int>("Counts/orders-today");
		_usersCount = await api.Get<int>("Counts/users");
		_companiesCount = await api.Get<int>("Counts/companies");
		_turnover = await api.Get<decimal>("Counts/turnover");
	}

	private string GetRevenue() => $"{_turnover:F2} BYN";
	private static string GetDate(ActivityRecord record) => record.Date.ToString("MM.dd - HH:mm");
}

public record CountsInfo(int Orders, int Users, int Companies, decimal Turnover, ICollection<ActivityRecord> Activities);

