using System.Text.Json;

namespace GeoNorge.DownloadClient.Cli;

internal sealed class CredentialStore
{
    private readonly string _filePath;

    public CredentialStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _filePath = Path.Combine(appData, "GeoNorge.DownloadClient", "credentials.json");
    }

    public StoredCredentials? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        string json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<StoredCredentials>(json);
    }

    public void Save(StoredCredentials credentials)
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}

internal sealed class StoredCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

internal sealed class BearerTokenStore
{
    private readonly string _filePath;

    public BearerTokenStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _filePath = Path.Combine(appData, "GeoNorge.DownloadClient", "bearer-token.json");
    }

    public StoredBearerToken? LoadValid()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        string json = File.ReadAllText(_filePath);
        StoredBearerToken? token = JsonSerializer.Deserialize<StoredBearerToken>(json);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return null;
        }

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return null;
        }

        return token;
    }

    public void Save(StoredBearerToken token)
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(token, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}

internal sealed class StoredBearerToken
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
