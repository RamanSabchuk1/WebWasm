using System.Text.Json;

namespace WebWasm.Helpers;

/// <summary>
/// Value format of browser localStorage, kept identical to Blazored.LocalStorage 4.x (which the app used before),
/// so values already saved by users (token, filters, sort state) stay readable.
/// </summary>
public static class StorageFormat
{
	public static string Serialize<T>(T value, JsonSerializerOptions options) =>
		JsonSerializer.Serialize(value, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));

	/// <summary>Missing or blank value → default. A string saved raw (not JSON) is returned as is, like Blazored did.</summary>
	public static T? Deserialize<T>(string? stored, JsonSerializerOptions options)
	{
		if (string.IsNullOrWhiteSpace(stored))
		{
			return default;
		}

		try
		{
			return JsonSerializer.Deserialize(stored, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
		}
		catch (JsonException e) when (e.Path == "$" && typeof(T) == typeof(string))
		{
			return (T)(object)stored;
		}
	}
}
