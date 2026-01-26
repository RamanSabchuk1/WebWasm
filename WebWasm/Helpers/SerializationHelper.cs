using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebWasm.Helpers;

public static class SerializationHelper
{
    public static JsonSerializerOptions SerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
