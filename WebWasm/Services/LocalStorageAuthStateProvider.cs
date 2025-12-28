using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace WebWasm.Services;

public class LocalStorageAuthStateProvider(ILocalStorageService localStorage, EncryptionService encryptionService) : AuthenticationStateProvider
{
    private const string TokenStorageKey = "encryptedAuthToken";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
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
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(decryptedToken);
                identity = new ClaimsIdentity(jwt.Claims, "jwt");
            }
            catch
            {
                // If token is invalid, treat as unauthenticated
                await MarkUserAsLoggedOut();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public async ValueTask MarkUserAsAuthenticated(string rawJwt)
    {
        var encryptedToken = encryptionService.Encrypt(rawJwt);
        await localStorage.SetItemAsync(TokenStorageKey, encryptedToken);
        
        var authState = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(authState);
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