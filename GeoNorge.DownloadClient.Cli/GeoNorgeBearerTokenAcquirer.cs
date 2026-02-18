using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoNorge.DownloadClient.Cli;

internal static class GeoNorgeBearerTokenAcquirer
{
    public static async Task<TokenAcquisitionResult> AcquireBearerTokenAsync(
        string baseUrl,
        string metadataUuid,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "geonorge_kartkatalog",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid email profile"
        };

        using var content = new FormUrlEncodedContent(tokenRequest);
        using HttpResponseMessage tokenResponse = await httpClient.PostAsync(
            "https://auth2.geoid.no/realms/geoid/protocol/openid-connect/token",
            content,
            cancellationToken);

        string payload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to acquire bearer token from GeoID: {payload}");
        }

        TokenResponse? token = JsonSerializer.Deserialize<TokenResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("GeoID token response did not contain access_token.");
        }

        int expiresIn = token.ExpiresIn > 0 ? token.ExpiresIn : 300;
        return new TokenAcquisitionResult(
            token.AccessToken,
            DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }

    internal sealed record TokenAcquisitionResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
