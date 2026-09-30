using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace WebWasm.Helpers;

/// <summary>
/// Reads JWT claims without validating the signature, like <c>JwtSecurityTokenHandler.ReadJwtToken</c>:
/// the server validates the token, the client only needs to know it is well-formed.
/// </summary>
public static class JwtReader
{
	/// <summary>Claims of a compact JWS (header.payload.signature), or null if the token is not a well-formed JWT.</summary>
	public static IReadOnlyList<Claim>? ReadClaims(string token)
	{
		var parts = token.Split('.');
		if (parts.Length != 3 || !IsJsonObject(parts[0]))
		{
			return null;
		}

		try
		{
			using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));
			if (payload.RootElement.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			var claims = new List<Claim>();
			foreach (var property in payload.RootElement.EnumerateObject())
			{
				if (property.Value.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in property.Value.EnumerateArray())
					{
						claims.Add(new Claim(property.Name, ClaimValue(item)));
					}
				}
				else
				{
					claims.Add(new Claim(property.Name, ClaimValue(property.Value)));
				}
			}
			return claims;
		}
		catch (Exception ex) when (ex is FormatException or JsonException)
		{
			return null;
		}
	}

	private static bool IsJsonObject(string base64Url)
	{
		try
		{
			using var doc = JsonDocument.Parse(DecodeBase64Url(base64Url));
			return doc.RootElement.ValueKind == JsonValueKind.Object;
		}
		catch (Exception ex) when (ex is FormatException or JsonException)
		{
			return false;
		}
	}

	private static string ClaimValue(JsonElement value) => value.ValueKind switch
	{
		JsonValueKind.String => value.GetString()!,
		_ => value.GetRawText(),
	};

	private static byte[] DecodeBase64Url(string value)
	{
		var base64 = new StringBuilder(value.Length + 3).Append(value).Replace('-', '+').Replace('_', '/');
		base64.Append('=', (4 - value.Length % 4) % 4);
		return Convert.FromBase64String(base64.ToString());
	}
}
