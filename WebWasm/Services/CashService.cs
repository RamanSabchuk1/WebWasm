using System.Collections.Concurrent;
using WebWasm.Models;

namespace WebWasm.Services;

public class CashService<T>(ApiClient apiClient, ToastService toastService, LoadingService loadingService) where T : class
{
    private readonly TimeSpan _defaultExpirationTime = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, TimeSpan> _typeExpiration = new()
    {
        [nameof(Suggestion)] = TimeSpan.FromMinutes(5),
        [nameof(MaterialType)] = TimeSpan.FromMinutes(30),
        [nameof(Region)] = TimeSpan.FromMinutes(30),
        [nameof(Vehicle)] = TimeSpan.FromMinutes(5),
        [nameof(Driver)] = TimeSpan.FromMinutes(5),
        [nameof(Producer)] = TimeSpan.FromMinutes(5),
        [nameof(Company)] = TimeSpan.FromMinutes(5),
        [nameof(User)] = TimeSpan.FromMinutes(5),
        [nameof(Order)] = TimeSpan.FromSeconds(150),
        [nameof(CalculationInfo)] = TimeSpan.FromSeconds(150),
    };

    private readonly ConcurrentDictionary<string, Func<Task<T[]>>> _typeFetch = new()
    {
        [nameof(Order)] = async () => await apiClient.Get<T[]>("Orders"),
                        //[nameof(CalculationInfo)] = async () => await apiClient.Get<T[]>("Orders/info"),
        [nameof(User)] = async () => await apiClient.Get<T[]>("Users/all"),
        [nameof(Role)] = async () => await apiClient.Get<T[]>("Users/roles​"),
        [nameof(Company)] = async () => await apiClient.Get<T[]>("Companies"),

        [nameof(Producer)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(Vehicle)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(Driver)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(CreditCardInfo)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(Region)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(MaterialType)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(DeviceToken)] = async () => await apiClient.Get<T[]>("Orders"),
        [nameof(Suggestion)] = async () => await apiClient.Get<T[]>("Orders")
    };

    private readonly ConcurrentDictionary<string, CashedInfo<T>> _cachedData = [];

    public async ValueTask<T[]> GetData()
    {
        var key = typeof(T).Name;
        if (!(_cachedData.TryGetValue(key, out var cachedInfo) && _typeFetch.TryGetValue(typeof(T).Name, out var fetchFunc)))
        {
            return [];
        }

        var expirationTime = _typeExpiration.GetValueOrDefault(key, _defaultExpirationTime);
        if (DateTime.UtcNow - cachedInfo.Cached <= expirationTime)
        {
            return cachedInfo.Data;
        }

        var result = await FetchData(loadingService, toastService, fetchFunc);
        _cachedData[key] = new CashedInfo<T>(DateTime.UtcNow, result);
        return result;
    }

    private static async Task<T[]> FetchData(LoadingService loadingService, ToastService toastService, Func<Task<T[]>> fetchFunc)
    {
        T[] result = [];
        await loadingService.ExecuteWithLoading(async () =>
        {
            try
            {
                result = await fetchFunc();
            }
            catch (Exception ex)
            {
                toastService.ShowError($"Failed to load {typeof(T).Name}: {ex.Message}");
            }
        });

        return result;
    }
}

public record CashedInfo<T>(DateTime Cached, T[] Data) where T : class
{
    public T[] Data { get; set; } = Data;
    public DateTime Cached { get; set; } = Cached;
}