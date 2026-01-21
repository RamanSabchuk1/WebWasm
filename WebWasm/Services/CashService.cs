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
        [nameof(Suggestion)] = async () => await apiClient.GetSuggestionsAsync(),
        [nameof(MaterialType)] = async () => await apiClient.GetMaterialTypesAsync(),
        [nameof(Region)] = async () => await apiClient.GetRegionsAsync(),
        [nameof(Vehicle)] = async () => await apiClient.GetVehiclesAsync(),
        [nameof(Driver)] = async () => await apiClient.GetDriversAsync(),
        [nameof(Producer)] = async () => await apiClient.GetProducersAsync(),
        [nameof(Company)] = async () => await apiClient.GetCompaniesAsync(),
        [nameof(User)] = async () => await apiClient.GetUsersAsync(),
        [nameof(Order)] = async () => await apiClient.GetOrdersAsync(),
        [nameof(CalculationInfo)] = TimeSpan.FromSeconds(150),
    };

    private readonly ConcurrentDictionary<string, CashedInfo<T>> _cachedData = [];

    public async ValueTask<T[]> GetData()
    {
        if (!(_cachedData.TryGetValue(typeof(T).Name, out var cachedInfo) && _typeFetch.TryGetValue(typeof(T).Name, out var fetchFunc)))
        {
            return [];
        }

        await loadingService.ExecuteWithLoading(async () =>
        {
            try
            {
                await apiClient.Post($"Supports/suggestion/apply?suggestionId={suggestionId}");
                toastService.ShowSuccess("Suggestion applied successfully!");
                await LoadSuggestions();
            }
            catch(Exception ex)
            {
                toastService.ShowError($"Failed to apply suggestion: {ex.Message}");
            }
        });

        var expirationTime = _typeExpiration.GetValueOrDefault(typeof(T).Name, _defaultExpirationTime);
        return DateTime.UtcNow - cachedInfo.Cached <= expirationTime
            ? cachedInfo.Data
            : [];
    }
}

public record CashedInfo<T>(DateTime Cached, T[] Data) where T : class
{
    public T[] Data { get; set; } = Data;
    public DateTime Cached { get; set; } = Cached;
}