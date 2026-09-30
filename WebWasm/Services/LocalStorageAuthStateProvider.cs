using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using WebWasm.Helpers;

namespace WebWasm.Services;

public class LocalStorageAuthStateProvider(LocalStorageService localStorage, EncryptionService encryptionService) : AuthenticationStateProvider
{
	private const string TokenStorageKey = "encryptedAuthToken";

	public override Task<AuthenticationState> GetAuthenticationStateAsync() => ReadState();

	/// <summary>Token for API calls: empty when there is none or it is not a valid JWT (then the user is logged out).</summary>
	public async ValueTask<string> GetValidJwt()
	{
		var state = await ReadState();
		return state.User.Identity?.IsAuthenticated == true ? await GetRawJwt() : string.Empty;
	}

	private async Task<AuthenticationState> ReadState()
	{
		var encryptedToken = await localStorage.GetItemAsync<string>(TokenStorageKey);

		if (string.IsNullOrWhiteSpace(encryptedToken))
		{
			return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Not authenticated
		}

		var decryptedToken = encryptionService.Decrypt(encryptedToken);

		var identity = new ClaimsIdentity();
		if (!string.IsNullOrWhiteSpace(decryptedToken))
		{
			var claims = JwtReader.ReadClaims(decryptedToken);
			if (claims is null)
			{
				// If token is invalid, treat as unauthenticated
				await MarkUserAsLoggedOut();
				return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
			}

			identity = new ClaimsIdentity(claims, "jwt");
		}

		var user = new ClaimsPrincipal(identity);
		return new AuthenticationState(user);
	}

	public async ValueTask MarkUserAsAuthenticated(string rawJwt)
	{
		var encryptedToken = encryptionService.Encrypt(rawJwt);
		await localStorage.SetItemAsync(TokenStorageKey, encryptedToken);

		NotifyAuthenticationStateChanged(ReadState());
	}

	public async ValueTask MarkUserAsLoggedOut()
	{
		await localStorage.RemoveItemAsync(TokenStorageKey);

		var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
		NotifyAuthenticationStateChanged(authState);
	}

	public async ValueTask<string> GetRawJwt()
	{
		var encryptedToken = await localStorage.GetItemAsync<string>(TokenStorageKey);

		if (string.IsNullOrWhiteSpace(encryptedToken))
		{
			return string.Empty;
		}

		return encryptionService.Decrypt(encryptedToken);
	}
}
