using System.Text.Json;
using Microsoft.JSInterop;
using WebWasm.Helpers;

namespace WebWasm.Services;

/// <summary>Browser localStorage with values in the app's JSON format (<see cref="StorageFormat"/>).</summary>
public class LocalStorageService(IJSRuntime js, JsonSerializerOptions options)
{
	public async ValueTask<T?> GetItemAsync<T>(string key)
	{
		string? stored;
		try
		{
			stored = await js.InvokeAsync<string?>("localStorage.getItem", key);
		}
		catch (JSException ex)
		{
			Console.WriteLine($"localStorage.getItem('{key}') failed: {ex.Message}");
			return default;
		}

		return StorageFormat.Deserialize<T>(stored, options);
	}

	/// <summary>
	/// Saved value, or <paramref name="fallback"/> when there is none or it no longer parses (format changed between
	/// versions): a broken saved filter must not break the page.
	/// </summary>
	public async ValueTask<T> GetItemOrDefaultAsync<T>(string key, T fallback)
	{
		try
		{
			return await GetItemAsync<T>(key) ?? fallback;
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"localStorage '{key}' is not readable, using default: {ex.Message}");
			return fallback;
		}
	}

	public async ValueTask SetItemAsync<T>(string key, T value)
	{
		var json = StorageFormat.Serialize(value, options);
		try
		{
			await js.InvokeVoidAsync("localStorage.setItem", key, json);
		}
		catch (JSException ex)
		{
			// Storage full or disabled (private mode): the app keeps working, the value is just not remembered.
			Console.WriteLine($"localStorage.setItem('{key}') failed: {ex.Message}");
		}
	}

	public async ValueTask RemoveItemAsync(string key)
	{
		try
		{
			await js.InvokeVoidAsync("localStorage.removeItem", key);
		}
		catch (JSException ex)
		{
			Console.WriteLine($"localStorage.removeItem('{key}') failed: {ex.Message}");
		}
	}
}
