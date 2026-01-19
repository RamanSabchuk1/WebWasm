using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebWasm.Components;

namespace WebWasm.Services;

public class ApiClient(IHttpClientFactory httpClientFactory, LocalStorageAuthStateProvider authStateProvider)
{
	public const string Authorization = nameof(Authorization);
	private const string Bearer = nameof(Bearer);
	//private const string BaseAddress = "https://kliffort.com/api/dev/";
	private const string BaseAddress = "https://localhost:7231/";

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
		return await response.Content.ReadFromJsonAsync<TResponse>() ?? throw new Exception($"Failed to get {endpoint}.");
	}

	public async ValueTask<TResponse> Post<TRequest, TResponse>(string endpoint, TRequest data)
	{
		var response = await PostInternal(endpoint, data);
		return await response.Content.ReadFromJsonAsync<TResponse>() ?? throw new Exception($"Failed to get {endpoint}.");
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

	private async ValueTask<HttpResponseMessage> PostInternal<TRequest>(string endpoint, TRequest data, bool readResponse = false)
	{
		var client = await GetHttpClient();

		var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
		{
			Content = JsonContent.Create(data)
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
		var client = httpClientFactory.CreateClient();
		client.BaseAddress = new Uri(BaseAddress);

		var authState = await authStateProvider.GetAuthenticationStateAsync();
		var token = await authStateProvider.GetRawJwt();
		if (authState.User.Identity?.IsAuthenticated == true && token != string.Empty)
		{
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Bearer, token);
		}
		else if (token != string.Empty)
		{
			await authStateProvider.MarkUserAsLoggedOut();
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
				throw new UnauthorizedAccessException($"Request to {endpoint} was unauthorized.");
			}

			var content = await response.Content.ReadAsStringAsync();
			throw new Exception($"Request to {endpoint} failed with status code {response.StatusCode}: {content}");
		}

		if (response.Headers.TryGetValues(Authorization, out var values))
		{
			var rawToken = values.FirstOrDefault();
			var newToken = rawToken is null || rawToken.Length < 10 ? string.Empty : rawToken[7..];
			if (newToken != string.Empty)
			{
				await authStateProvider.MarkUserAsAuthenticated(newToken);
			}
		}
	}

	private static StringContent CreateJsonContent<T>(T data)
	{
		var json = JsonSerializer.Serialize(data);
		return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
	}
}