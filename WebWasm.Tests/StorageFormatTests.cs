using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using WebWasm.Helpers;

namespace WebWasm.Tests;

public class StorageFormatTests
{
	public record SortState(string? ColumnKey = null, bool Descending = false);
	public record FilterState(string Preset, DateOnly? From, DateOnly? To);

	// Same settings as SerializationHelper.SerializerOptions(), with the reflection resolver instead of the app's source-gen context.
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
		WriteIndented = false,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
	};

	/// <summary>Browser localStorage as seen through JS interop: what Blazored writes lands here as raw strings.</summary>
	private sealed class FakeStorageJs : IJSRuntime
	{
		public Dictionary<string, string> Items { get; } = [];

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => identifier switch
		{
			"localStorage.setItem" => Done<TValue>(Items[(string)args![0]!] = (string)args[1]!),
			"localStorage.getItem" => ValueTask.FromResult((TValue)(object?)Items.GetValueOrDefault((string)args![0]!)!),
			_ => throw new NotSupportedException(identifier)
		};

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
			InvokeAsync<TValue>(identifier, args);

		private static ValueTask<TValue> Done<TValue>(object _) => ValueTask.FromResult(default(TValue)!);
	}

	private static (ILocalStorageService Blazored, FakeStorageJs Js) CreateBlazored()
	{
		var js = new FakeStorageJs();
		var services = new ServiceCollection()
			.AddSingleton<IJSRuntime>(js)
			// Blazored adds its own converters to the options, so it gets a fresh copy of the same settings.
			.AddBlazoredLocalStorage(o => o.JsonSerializerOptions = new JsonSerializerOptions(Options))
			.BuildServiceProvider();
		return (services.GetRequiredService<ILocalStorageService>(), js);
	}

	public static TheoryData<object> Values =>
	[
		"plain text",
		"with \"quotes\" and кириллица",
		new SortState("name", true),
		new SortState(),
		new FilterState("month", new DateOnly(2026, 9, 1), null),
	];

	[Theory]
	[MemberData(nameof(Values))]
	public async Task Serialize_SameAsBlazored(object value)
	{
		var (blazored, js) = CreateBlazored();

		await SetWithBlazored(blazored, value);

		Assert.Equal(js.Items["k"], Serialize(value));
	}

	[Theory]
	[MemberData(nameof(Values))]
	public async Task Deserialize_ReadsWhatBlazoredSaved(object value)
	{
		var (blazored, js) = CreateBlazored();

		await SetWithBlazored(blazored, value);

		Assert.Equal(value, Deserialize(value.GetType(), js.Items["k"]));
	}

	[Fact]
	public async Task Deserialize_RawStringNotJson_SameAsBlazored()
	{
		var (blazored, js) = CreateBlazored();
		js.Items["k"] = "abc+/=";

		Assert.Equal(await blazored.GetItemAsync<string>("k"), StorageFormat.Deserialize<string>("abc+/=", Options));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Deserialize_Missing_ReturnsDefault(string? stored)
	{
		Assert.Null(StorageFormat.Deserialize<string>(stored, Options));
		Assert.Null(StorageFormat.Deserialize<SortState>(stored, Options));
	}

	[Fact]
	public void Deserialize_BrokenJsonForRecord_Throws()
	{
		// Callers catch this (they did with Blazored too): the saved state is ignored.
		Assert.ThrowsAny<JsonException>(() => StorageFormat.Deserialize<SortState>("{not json", Options));
	}

	private static ValueTask SetWithBlazored(ILocalStorageService blazored, object value) => value switch
	{
		string v => blazored.SetItemAsync("k", v),
		SortState v => blazored.SetItemAsync("k", v),
		FilterState v => blazored.SetItemAsync("k", v),
		_ => throw new ArgumentException(value.GetType().Name)
	};

	private static string Serialize(object value) => value switch
	{
		string v => StorageFormat.Serialize(v, Options),
		SortState v => StorageFormat.Serialize(v, Options),
		FilterState v => StorageFormat.Serialize(v, Options),
		_ => throw new ArgumentException(value.GetType().Name)
	};

	private static object? Deserialize(Type type, string stored) =>
		type == typeof(string) ? StorageFormat.Deserialize<string>(stored, Options)
		: type == typeof(SortState) ? StorageFormat.Deserialize<SortState>(stored, Options)
		: StorageFormat.Deserialize<FilterState>(stored, Options);
}
