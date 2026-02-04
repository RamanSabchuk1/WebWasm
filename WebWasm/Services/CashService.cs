using System.Collections.Concurrent;
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
	};

	private readonly ConcurrentDictionary<string, Func<object?, Task<JsonElement>>> _typeFetch = new()
	{
		[nameof(Order)] = async _ => await apiClient.Get<JsonElement>("Orders"),
		[nameof(CalculationInfo)] = async args => await apiClient.Post<CalculationInfoRequest, JsonElement>("Orders/info", args as CalculationInfoRequest ?? throw new NotSupportedException()),
		[nameof(User)] = async _ => await apiClient.Get<JsonElement>("Users/all"),
		[nameof(Role)] = async _ => await apiClient.Get<JsonElement>("Users/roles"),
		[nameof(Company)] = async _ => await apiClient.Get<JsonElement>("Companies"),
		[nameof(Producer)] = async _ => await apiClient.Get<JsonElement>("Producers"),
		[nameof(Vehicle)] = async _ => await apiClient.Get<JsonElement>("Companies/vehicle"),
		[nameof(Driver)] = async _ => await apiClient.Get<JsonElement>("Drivers"),
		[nameof(CreditCardInfo)] = async _ => await apiClient.Get<JsonElement>("Payments/all-cards"),
		[nameof(Region)] = async _ => await apiClient.Get<JsonElement>("Regions"),
		[nameof(MaterialType)] = async _ => await apiClient.Get<JsonElement>("MaterialTypes"),
		[nameof(DeviceToken)] = async _ => await apiClient.Get<JsonElement>("DeviceTokens"),
		[nameof(Suggestion)] = async _ => await apiClient.Get<JsonElement>("Supports/suggestion/all"),
		[nameof(UserInfo)] = async _ => await apiClient.Get<JsonElement>("/Users")
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
			try
			{
				return cachedInfo.Data.Deserialize<T[]>(_serOptions) ?? [];
			}
			catch (Exception ex)
			{
				toastService.ShowError($"Failed to deserialize {key}: {ex.Message}");
				return [];
			}
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
        if(!_typeFetch.TryGetValue(key, out var fetchFunc))
        {
            toastService.ShowError("Fetch user Info not found");
			return null;
        }

        if(!_cachedData.TryGetValue(key, out var cachedInfo))
        {
            cachedInfo = new CashedInfo(DateTime.MinValue, default);
        }

        var expirationTime = _typeExpiration.GetValueOrDefault(key, _defaultExpirationTime);
        if(DateTime.UtcNow - cachedInfo.Cached <= expirationTime && useCash)
        {
            try
            {
				return cachedInfo.Data.Deserialize<UserInfo>(_serOptions);
            }
            catch(Exception ex)
            {
                toastService.ShowError($"Failed to deserialize {key}: {ex.Message}");
                return null;
            }
        }

		UserInfo? result = null;
        await loadingService.ExecuteWithLoading(async () => {
            try
            {
                var response = await fetchFunc(default);
                var result = response.Deserialize<UserInfo>(_serOptions);
                if(result is not null)
                {
                    cachedInfo = new CashedInfo(DateTime.UtcNow, response);
                };
            }
            catch(Exception ex)
            {
                toastService.ShowError($"Failed to load {key}: {ex.Message}");
            }
        });


        if(cachedInfo is not null)
        {
            _cachedData[key] = cachedInfo;
        }

        return result;
    }

    private async Task<(CashedInfo?, T[])> FetchData<T>(string key, Func<object?, Task<JsonElement>> fetchFunc, bool useCash)
	{
		T[] result = [];
		CashedInfo? cashValue = null;
		await loadingService.ExecuteWithLoading(async () => {
			try
			{
				var args = await BuildCalculationInfoRequest(key, useCash);
				var response = await fetchFunc(args);
				var res = response.Deserialize<T[]>(_serOptions);
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