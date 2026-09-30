using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Home(CashService cashService, ApiClient api, LocalStorageService localStorage)
{
	private const string TurnoverFilterStorageKey = "home_turnover_filter";

	private static readonly (string Key, string Label)[] _turnoverPresets =
	[
		("1w", "1W"),
		("2w", "2W"),
		("1m", "1M"),
		("2m", "2M"),
		("1q", "1Q"),
		("all", "All time"),
	];

	private int _ordersTodayCount;
	private int _usersCount;
	private int _companiesCount;
	private TurnoverInfo _turnover = new(0, 0);
	private ActivityRecord[] _activityRecords = [];

	// _turnoverTo — ВКЛЮЧИТЕЛЬНАЯ последняя дата периода (на API уходит to+1day exclusive).
	private string _turnoverPreset = "all";
	private DateOnly? _turnoverFrom;
	private DateOnly? _turnoverTo;

	protected override async Task OnInitializedAsync()
	{
		await RestoreTurnoverFilter();
		await LoadData();
	}

	private async ValueTask LoadData()
	{
		var activities = cashService.GetData<ActivityRecord>().AsTask();
		var ordersToday = api.Get<int>("Counts/orders-today").AsTask();
		var users = api.Get<int>("Counts/users").AsTask();
		var companies = api.Get<int>("Counts/companies").AsTask();
		await Task.WhenAll(activities, ordersToday, users, companies, LoadTurnover());

		_activityRecords = activities.Result;
		_ordersTodayCount = ordersToday.Result;
		_usersCount = users.Result;
		_companiesCount = companies.Result;
	}

	private async Task LoadTurnover()
	{
		var query = string.Empty;
		if (_turnoverFrom is not null || _turnoverTo is not null)
		{
			var parts = new List<string>(capacity: 2);
			if (_turnoverFrom is not null)
			{
				parts.Add($"from={FormatDate(_turnoverFrom)}");
			}
			if (_turnoverTo is not null)
			{
				parts.Add($"to={FormatDate(_turnoverTo.Value.AddDays(1))}");
			}
			query = "?" + string.Join("&", parts);
		}

		_turnover = await api.Get<TurnoverInfo>($"Counts/turnover{query}");
	}

	private async Task ApplyTurnoverPreset(string preset)
	{
		_turnoverPreset = preset;
		if (preset != "custom")
		{
			ApplyPresetDates(preset);
		}
		await SaveTurnoverFilter();
		await LoadTurnover();
	}

	private void ApplyPresetDates(string preset)
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		(_turnoverFrom, _turnoverTo) = preset switch
		{
			"1w" => (today.AddDays(-7), today),
			"2w" => (today.AddDays(-14), today),
			"1m" => (today.AddMonths(-1), today),
			"2m" => (today.AddMonths(-2), today),
			"1q" => (today.AddMonths(-3), today),
			_ => ((DateOnly?)null, (DateOnly?)null),
		};
	}

	private async Task OnTurnoverFromChanged(ChangeEventArgs e)
	{
		_turnoverFrom = DateOnly.TryParse(e.Value?.ToString(), out var from) ? from : null;
		_turnoverPreset = "custom";
		await SaveTurnoverFilter();
		await LoadTurnover();
	}

	private async Task OnTurnoverToChanged(ChangeEventArgs e)
	{
		_turnoverTo = DateOnly.TryParse(e.Value?.ToString(), out var to) ? to : null;
		_turnoverPreset = "custom";
		await SaveTurnoverFilter();
		await LoadTurnover();
	}

	private async Task RestoreTurnoverFilter()
	{
		var saved = await localStorage.GetItemOrDefaultAsync<TurnoverFilterState?>(TurnoverFilterStorageKey, null);
		if (saved is null)
		{
			return;
		}

		_turnoverPreset = saved.Preset ?? "all";
		if (_turnoverPreset is "custom")
		{
			_turnoverFrom = saved.From;
			_turnoverTo = saved.To;
		}
		else
		{
			// Пресеты относительные («от текущей даты») — пересчитываем, а не берём сохранённые даты
			ApplyPresetDates(_turnoverPreset);
		}
	}

	private async Task SaveTurnoverFilter()
	{
		await localStorage.SetItemAsync(TurnoverFilterStorageKey, new TurnoverFilterState(_turnoverPreset, _turnoverFrom, _turnoverTo));
	}

	private string GetTurnoverPeriodLabel()
	{
		return _turnoverPreset switch
		{
			"custom" when _turnoverFrom is not null || _turnoverTo is not null => $"{FormatDate(_turnoverFrom) ?? "…"} — {FormatDate(_turnoverTo) ?? "…"}",
			"custom" => "all time",
			var p => _turnoverPresets.FirstOrDefault(x => x.Key == p).Label ?? "all time",
		};
	}

	private static string? FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd");

	private string GetRevenue() => $"{_turnover.Turnover:F2} BYN";
	private string GetProfit() => $"{_turnover.Profit:F2} BYN";
	private static string GetDate(ActivityRecord record) => record.Date.ToString("MM.dd - HH:mm");
}

