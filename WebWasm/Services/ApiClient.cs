using WebWasm.Components;
using System.Text.Json;
using System.Net.Http.Json;

namespace WebWasm.Services;

public class ApiClient(IHttpClientFactory httpClientFactory, LocalStorageAuthStateProvider authStateProvider)
{
    private const string Bearer = nameof(Bearer);
    private const string BaseAddress = "https://kliffort.com/api/dev/";
    //private const string BaseAddress = "https://localhost:7231/";

    private const string Auth = nameof(Auth);

    public async ValueTask Login(Login.LoginModel login)
    {
        var token = (await Post<Login.LoginModel, Login.TokenResponse>(Auth, login)).Token;
        await authStateProvider.MarkUserAsAuthenticated(token);
    }

    public async ValueTask<TResponse> Get<TResponse>(string endpoint)
    {
        var client = await GetHttpClient();
        var response = await client.GetAsync(endpoint);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>() ?? throw new Exception($"Failed to get {endpoint}.");
    }

    public async ValueTask<TResponse> Post<TRequest, TResponse>(string endpoint, TRequest data)
    {
        var response = await PostInternal(endpoint, data);
        return await response.Content.ReadFromJsonAsync<TResponse>() ?? throw new Exception($"Failed to get {endpoint}.");
    }

    public async ValueTask Post<TRequest>(string endpoint, TRequest data)
    {
        await PostInternal(endpoint, data);
    }

    private async ValueTask<HttpResponseMessage> PostInternal<TRequest>(string endpoint, TRequest data)
    {
        var client = await GetHttpClient();
        var response = await client.PostAsJsonAsync(endpoint, data);

        response.EnsureSuccessStatusCode();
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

    private static StringContent CreateJsonContent<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}