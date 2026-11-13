using Microsoft.AspNetCore.Components;
using WebWasm.Models;
using WebWasm.Services;

namespace WebWasm.Pages;

public partial class Home(CashService cashService, ApiClient api, LocalStorageService localStorage, ToastService toastService)
{
	private const string TurnoverFilterStorageKey = "home_turnover_filter";

	// Labels: T.Home.Preset(key).
	private static readonly string[] _turnoverPresets = ["1w", "2w", "1m", "2m", "1q", "all"];

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
		// Каждый счётчик грузится сам по себе: сбой одного — тост и 0, остальные показываются.
		var activities = Safe(cashService.GetData<ActivityRecord>().AsTask(), [], T.Home.ActivityFailed);
		var ordersToday = Safe(api.Get<int>("Counts/orders-today").AsTask(), 0, T.Home.OrdersTodayFailed);
		var users = Safe(api.Get<int>("Counts/users").AsTask(), 0, T.Home.UsersFailed);
		var companies = Safe(api.Get<int>("Counts/companies").AsTask(), 0, T.Home.CompaniesFailed);
		await Task.WhenAll(activities, ordersToday, users, companies, LoadTurnover());

		_activityRecords = activities.Result;
		_ordersTodayCount = ordersToday.Result;
		_usersCount = users.Result;
		_companiesCount = companies.Result;
	}

	private async Task<TResult> Safe<TResult>(Task<TResult> load, TResult fallback, Func<string, string> errorMessage)
	{
		try
		{
			return await load;
		}
		catch (Exception ex)
		{
			toastService.ShowError(errorMessage(ex.Message));
			return fallback;
		}
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

		_turnover = await Safe(api.Get<TurnoverInfo>($"Counts/turnover{query}").AsTask(), new TurnoverInfo(0, 0), T.Home.TurnoverFailed);
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
			"custom" when _turnoverFrom is not null || _turnoverTo is not null => $"{DisplayDate(_turnoverFrom)} — {DisplayDate(_turnoverTo)}",
			"custom" => T.Home.AllTime,
			var p => T.Home.Preset(p),
		};
	}

	// yyyy-MM-dd: the API query and the value of <input type="date">, not for display.
	private static string? FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd");

	private string DisplayDate(DateOnly? date) => date is { } d ? T.FormatDate(d) : "…";

	private string FormatCount(int value) => T.FormatNumber((decimal)value, 0);
}

