using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using WebWasm.Helpers;
using WebWasm.Models;

namespace WebWasm.Services;

public class CashService(ApiClient apiClient, ToastService toastService, LoadingService loadingService)
{
	private static readonly JsonSerializerOptions _serOptions = SerializationHelper.SerializerOptions();

	private readonly TimeSpan _defaultExpirationTime = TimeSpan.FromMinutes(20);
	private readonly ConcurrentDictionary<string, TimeSpan> _typeExpiration = new()
	{
		[nameof(Suggestion)] = TimeSpan.FromMinutes(5),
		[nameof(Region)] = TimeSpan.FromMinutes(8),
		[nameof(Vehicle)] = TimeSpan.FromMinutes(5),
		[nameof(Driver)] = TimeSpan.FromMinutes(7),
		[nameof(Producer)] = TimeSpan.FromMinutes(5),
		[nameof(Company)] = TimeSpan.FromMinutes(6),
		[nameof(User)] = TimeSpan.FromMinutes(6),
		[nameof(Order)] = TimeSpan.FromMinutes(7),
		[nameof(CalculationInfo)] = TimeSpan.FromMinutes(7),
		[nameof(DriverSlot)] = TimeSpan.FromMinutes(5),
	};

	private readonly ConcurrentDictionary<string, Func<object?, Task<JsonElement>>> _typeFetch = new()
	{
		[nameof(Order)] = async _ => await apiClient.Get<JsonElement>("Orders"),
		[nameof(CalculationInfo)] = async args => await apiClient.Post<CalculationInfoRequest, JsonElement>("Orders/info", args as CalculationInfoRequest ?? throw new NotSupportedException()),
		[nameof(User)] = async _ => await apiClient.Get<JsonElement>("Users/all"),
		[nameof(Company)] = async _ => await apiClient.Get<JsonElement>("Companies"),
		[nameof(Producer)] = async _ => await apiClient.Get<JsonElement>("Producers"),
		[nameof(Vehicle)] = async _ => await apiClient.Get<JsonElement>("Companies/vehicle"),
		[nameof(Driver)] = async _ => await apiClient.Get<JsonElement>("Drivers"),
		[nameof(CreditCardInfo)] = async _ => await apiClient.Get<JsonElement>("Payments/all-cards"),
		[nameof(Region)] = async _ => await apiClient.Get<JsonElement>("Regions"),
		[nameof(MaterialType)] = async _ => await apiClient.Get<JsonElement>("MaterialTypes"),
		[nameof(DeviceToken)] = async _ => await apiClient.Get<JsonElement>("DeviceTokens"),
		[nameof(Suggestion)] = async _ => await apiClient.Get<JsonElement>("Supports/suggestion/all"),
		[nameof(UserInfo)] = async _ => await apiClient.Get<JsonElement>("Users"),
		[nameof(ActivityRecord)] = async _ => await apiClient.Get<JsonElement>("Counts/activities")
	};

	private readonly ConcurrentDictionary<string, CashedInfo> _cachedData = [];

	public async ValueTask<T[]> GetData<T>(bool useCash = true)
	{
		var key = typeof(T).Name;
		if (!_typeFetch.TryGetValue(key, out var fetchFunc))
		{
			return [];
		}

		if (!_cachedData.TryGetValue(key, out var cachedInfo))
		{
			cachedInfo = new CashedInfo(DateTime.MinValue, default);
		}

		var expirationTime = _typeExpiration.GetValueOrDefault(key, _defaultExpirationTime);
		if (DateTime.UtcNow - cachedInfo.Cached <= expirationTime && useCash)
		{
			return cachedInfo.Data.Deserialize(_serOptions.TypeInfo<T[]>()) ?? [];
		}

		var (cashValue, result) = await FetchData<T>(key, fetchFunc, useCash);
		if (cashValue is not null)
		{
			_cachedData[key] = cashValue;
		}

		return result;
	}

	public async ValueTask<UserInfo?> GetUserInfo(bool useCash = true)
	{
		var key = nameof(UserInfo);
		if (!_typeFetch.TryGetValue(key, out var fetchFunc))
		{
			toastService.ShowError("Fetch user Info not found");
			return null;
		}

		if (!_cachedData.TryGetValue(key, out var cachedInfo))
		{
			cachedInfo = new CashedInfo(DateTime.MinValue, default);
		}

		var expirationTime = _typeExpiration.GetValueOrDefault(key, _defaultExpirationTime);
		if (DateTime.UtcNow - cachedInfo.Cached <= expirationTime && useCash)
		{
			return cachedInfo.Data.Deserialize(_serOptions.TypeInfo<UserInfo>());
		}

		UserInfo? result = null;
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var response = await fetchFunc(default);
				result = response.Deserialize(_serOptions.TypeInfo<UserInfo>());
				if (result is not null)
				{
					cachedInfo = new CashedInfo(DateTime.UtcNow, response);
				}
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to load {key}: {ex.Message}");
			}
		});

		_cachedData[key] = cachedInfo;
		return result;
	}

	public async ValueTask<DriverSlot[]> GetSlots(Dictionary<Guid, List<Guid>> driverIds, bool useCash = true)
	{
		var key = nameof(DriverSlot);
		if (!_cachedData.TryGetValue(key, out var cachedInfo))
		{
			cachedInfo = new CashedInfo(DateTime.MinValue, default);
		}

		var expirationTime = _typeExpiration.GetValueOrDefault(key, _defaultExpirationTime);
		if (DateTime.UtcNow - cachedInfo.Cached <= expirationTime && useCash)
		{
			return cachedInfo.Data.Deserialize(_serOptions.TypeInfo<DriverSlot[]>()) ?? [];
		}

		List<DriverSlot> result = [];
		var loaded = false;
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				foreach (var (regionId, drivers) in driverIds)
				{
					var response = await apiClient.Get<DriverSlot[]>($"Drivers/slots/filter{GetArgs(drivers, regionId)}");
					result.AddRange(response);
				}
				loaded = true;
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to load {key}: {ex.Message}");
			}
		});

		DriverSlot[] slots = [.. result];
		// Before: the old empty entry was stored back, so slots were never served from cache.
		if (loaded)
		{
			_cachedData[key] = new CashedInfo(DateTime.UtcNow, JsonSerializer.SerializeToElement(slots, _serOptions.TypeInfo<DriverSlot[]>()));
		}

		return slots;

		static string GetArgs(List<Guid> drivers, Guid regionId)
		{
			var sb = new StringBuilder($"?regionId={regionId}");

			for (var i = 0; i < drivers.Count; i++)
			{
				var driver = drivers[i];
				sb.Append($"&driverIds={driver}");
			}

			return sb.ToString();

		}
	}

	public async ValueTask<DriverSlot[]> GetSlots(bool useCash = true)
	{
		var (users, companies, drivers) = await GetUsersCompaniesDrivers(useCash);
		return await GetSlots(drivers, users, companies, useCash);
	}

	/// <summary>Slots for already loaded lists (avoids fetching users, companies and drivers a second time).</summary>
	public async ValueTask<DriverSlot[]> GetSlots(Driver[] drivers, User[] users, Company[] companies, bool useCash = true)
	{
		var regionDrivers = DriverCompanies(drivers, users)
			.Join(companies,
				d => d.CompanyId,
				c => c.Id,
				(d, c) => new { DriverId = d.Driver.Id, c.RegionId })
			.GroupBy(x => x.RegionId)
			.ToDictionary(g => g.Key, g => g.Select(x => x.DriverId).ToList());

		return await GetSlots(regionDrivers, useCash);
	}

	/// <summary>The three lists behind driver/company lookups, fetched in parallel (different cache keys).</summary>
	public async ValueTask<(User[] Users, Company[] Companies, Driver[] Drivers)> GetUsersCompaniesDrivers(bool useCash = true)
	{
		var users = GetData<User>(useCash).AsTask();
		var companies = GetData<Company>(useCash).AsTask();
		var drivers = GetData<Driver>(useCash).AsTask();
		await Task.WhenAll(users, companies, drivers);
		return (users.Result, companies.Result, drivers.Result);
	}

	public async ValueTask<(Company, Driver)[]> GetDriverWithCompany()
	{
		var (users, companies, drivers) = await GetUsersCompaniesDrivers();

		var driverWithCompany = DriverCompanies(drivers, users)
			.Join(companies,
				d => d.CompanyId,
				c => c.Id,
				(d, c) => (c, d.Driver));

		return [.. driverWithCompany];
	}

	/// <summary>Company of each driver: from the driver's user (Users/all) if known, else from the driver's own UserInfo.</summary>
	private static IEnumerable<(Driver Driver, Guid CompanyId)> DriverCompanies(Driver[] drivers, User[] users)
	{
		// First user wins on duplicate UserInfo ids, as FirstOrDefault did before.
		var userByInfoId = new Dictionary<Guid, User>();
		foreach (var user in users)
		{
			if (user.UserInfo is not null)
			{
				userByInfoId.TryAdd(user.UserInfo.Id, user);
			}
		}

		foreach (var driver in drivers)
		{
			if (driver.UserInfo is null)
			{
				continue;
			}

			var companyId = userByInfoId.GetValueOrDefault(driver.UserInfo.Id)?.UserInfo?.Company?.Id ?? driver.UserInfo.Company?.Id;
			if (companyId is { } id)
			{
				yield return (driver, id);
			}
		}
	}

	private async Task<(CashedInfo?, T[])> FetchData<T>(string key, Func<object?, Task<JsonElement>> fetchFunc, bool useCash)
	{
		T[] result = [];
		CashedInfo? cashValue = null;
		await loadingService.ExecuteWithLoading(async () =>
		{
			try
			{
				var args = await BuildCalculationInfoRequest(key, useCash);
				var response = await fetchFunc(args);
				var res = response.Deserialize(_serOptions.TypeInfo<T[]>());
				if (res is not null)
				{
					cashValue = new CashedInfo(DateTime.UtcNow, response);
				}

				result = res ?? [];
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to load {key}: {ex.Message}");
			}
		});

		return (cashValue, result);
	}

	private async ValueTask<object?> BuildCalculationInfoRequest(string key, bool useCash)
	{
		if (key == nameof(CalculationInfo))
		{
			var orders = await GetData<Order>(useCash);
			return new CalculationInfoRequest([.. orders.Select(o => o.Id)]);
		}

		return null;
	}

}

public record CashedInfo(DateTime Cached, JsonElement Data)
{
	public JsonElement Data { get; set; } = Data;
	public DateTime Cached { get; set; } = Cached;
}
