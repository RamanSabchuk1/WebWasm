using System.IdentityModel.Tokens.Jwt;
using System.Text;
using WebWasm.Helpers;

namespace WebWasm.Tests;

public class JwtReaderTests
{
	private static string B64Url(string json) =>
		Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

	private static string Token(string payload) => $"{B64Url("""{"alg":"HS256","typ":"JWT"}""")}.{B64Url(payload)}.c2lnbmF0dXJl";

	private const string Payload = """
		{"sub":"42","name":"Иван Петров","role":["SuperAdmin","Admin"],"exp":1767225600,"admin":true,"extra":{"a":1}}
		""";

	[Fact]
	public void ReadClaims_MatchesJwtSecurityTokenHandler()
	{
		var token = Token(Payload);

		var expected = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Select(c => (c.Type, c.Value)).Order().ToArray();
		var actual = JwtReader.ReadClaims(token)!.Select(c => (c.Type, c.Value)).Order().ToArray();

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void ReadClaims_ArrayClaim_BecomesOneClaimPerItem()
	{
		var claims = JwtReader.ReadClaims(Token(Payload))!;

		Assert.Equal(["SuperAdmin", "Admin"], claims.Where(c => c.Type == "role").Select(c => c.Value));
	}

	[Fact]
	public void ReadClaims_PayloadNeedingPadding_IsDecoded()
	{
		// Lengths 1..3 mod 4 exercise every padding case of base64url.
		foreach (var name in new[] { "a", "ab", "abc", "abcd" })
		{
			var claims = JwtReader.ReadClaims(Token($$"""{"n":"{{name}}"}"""));
			Assert.Equal(name, Assert.Single(claims!).Value);
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("not-a-token")]
	[InlineData("a.b")]
	[InlineData("a.b.c.d")]
	[InlineData("!!!.@@@.###")]
	public void ReadClaims_Malformed_ReturnsNull(string token)
	{
		Assert.Null(JwtReader.ReadClaims(token));
	}

	[Fact]
	public void ReadClaims_PayloadNotJsonObject_ReturnsNull()
	{
		Assert.Null(JwtReader.ReadClaims(Token("[1,2]")));
		Assert.Null(JwtReader.ReadClaims($"{B64Url("[]")}.{B64Url("""{"a":1}""")}.x"));
	}

	[Fact]
	public void ReadClaims_TokenStoredByEncryptionService_RoundTrips()
	{
		var encryption = new WebWasm.Services.EncryptionService();
		var token = Token(Payload);

		var claims = JwtReader.ReadClaims(encryption.Decrypt(encryption.Encrypt(token)));

		Assert.Equal("42", claims!.Single(c => c.Type == "sub").Value);
	}
}
