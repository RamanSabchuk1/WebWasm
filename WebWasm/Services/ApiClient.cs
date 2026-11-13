using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using WebWasm.Components;
using WebWasm.Helpers;

namespace WebWasm.Services;

public class ApiClient(IHttpClientFactory httpClientFactory, LocalStorageAuthStateProvider authStateProvider, Preferences preferences)
{
	private static readonly JsonSerializerOptions _jsonOptions = SerializationHelper.SerializerOptions();
	public const string Authorization = nameof(Authorization);
	private const string Bearer = nameof(Bearer);

	/// <summary>Named client with the API address of the current environment (appsettings.json, registered in Program).</summary>
	public const string HttpClientName = "api";

	private const string Auth = nameof(Auth);

	public async ValueTask Login(Login.LoginModel login)
	{
		var token = (await Post<Login.LoginModel, Login.TokenResponse>(Auth, login)).Token;
		await authStateProvider.MarkUserAsAuthenticated(token);
	}

	public async ValueTask<TResponse> Get<TResponse>(string endpoint)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);

		await CheckResponseHeader(response, endpoint);
		return await ReadJson<TResponse>(response, endpoint);
	}

	public async ValueTask<TResponse> Post<TRequest, TResponse>(string endpoint, TRequest data)
	{
		var response = await PostInternal(endpoint, data);
		return await ReadJson<TResponse>(response, endpoint);
	}

	public async ValueTask Post<TRequest>(string endpoint, TRequest data)
	{
		await PostInternal(endpoint, data, true);
	}

	public async ValueTask Post(string endpoint)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);

		await CheckResponseHeader(response, endpoint);
	}

	/// <summary>
	/// POST без тела запроса, но с типизированным чтением ответа (например, Admin/secure-data/backfill,
	/// который принимает пустой POST и возвращает BackfillResult). Не путать с
	/// Post&lt;TRequest,TResponse&gt;, который требует тело запроса.
	/// </summary>
	public async ValueTask<TResponse> Post<TResponse>(string endpoint)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);

		await CheckResponseHeader(response, endpoint);
		return await ReadJson<TResponse>(response, endpoint);
	}

	public async ValueTask Put<TRequest>(string endpoint, TRequest data)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
		{
			Content = JsonBody(data)
		};

		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);
		await CheckResponseHeader(response, endpoint);
		_ = await response.Content.ReadAsStringAsync();
	}

	public async ValueTask Patch<TRequest>(string endpoint, TRequest data)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint)
		{
			Content = JsonBody(data)
		};

		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);
		await CheckResponseHeader(response, endpoint);
		_ = await response.Content.ReadAsStringAsync();
	}

	public async ValueTask Delete(string endpoint)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);
		_ = await response.Content.ReadAsStringAsync();
		await CheckResponseHeader(response, endpoint);
	}

	private async ValueTask<HttpResponseMessage> PostInternal<TRequest>(string endpoint, TRequest data, bool readResponse = false)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
		{
			Content = JsonBody(data)
		};

		request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

		var response = await client.SendAsync(request);

		await CheckResponseHeader(response, endpoint);

		if (readResponse)
		{
			_ = await response.Content.ReadAsStringAsync();
		}

		return response;
	}

	private async ValueTask<HttpClient> GetHttpClient()
	{
		var client = httpClientFactory.CreateClient(HttpClientName);

		var token = await authStateProvider.GetValidJwt();
		if (token != string.Empty)
		{
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Bearer, token);
		}

		return client;
	}

	private async ValueTask CheckResponseHeader(HttpResponseMessage response, string endpoint)
	{
		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
				await authStateProvider.MarkUserAsLoggedOut();
				throw new UnauthorizedAccessException(preferences.Texts.Common.SessionExpired);
			}

			var content = await response.Content.ReadAsStringAsync();
			Console.WriteLine($"{endpoint}: {(int)response.StatusCode} {content}");
			throw new HttpRequestException(preferences.Texts.Common.ServerError((int)response.StatusCode, content), null, response.StatusCode);
		}

		if (response.Headers.TryGetValues(Authorization, out var values))
		{
			var rawToken = values.FirstOrDefault();
			var newToken = rawToken is null || rawToken.Length < 10 ? string.Empty : rawToken[7..];
			if (newToken != string.Empty)
			{
				var currentToken = await authStateProvider.GetRawJwt();
				if (currentToken != newToken)
				{
					await authStateProvider.MarkUserAsAuthenticated(newToken);
				}
			}
		}
	}

	private static JsonContent JsonBody<T>(T data) => JsonContent.Create(data, _jsonOptions.TypeInfo<T>());

	private async ValueTask<T> ReadJson<T>(HttpResponseMessage response, string endpoint) =>
		await response.Content.ReadFromJsonAsync(_jsonOptions.TypeInfo<T>())
			?? throw new InvalidOperationException($"{preferences.Texts.Common.EmptyResponse} ({endpoint})");
}
