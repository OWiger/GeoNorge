using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net;

namespace GeoNorge.DownloadClient;

public sealed class GeoNorgeDownloadClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public GeoNorgeDownloadClient(
        HttpClient? httpClient = null,
        string baseUrl = "https://nedlasting.geonorge.no",
        string? username = null,
        string? password = null)
    {
        _httpClient = httpClient ?? new HttpClient();

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GeoNorge.DownloadClient", "1.0"));
        }

        if (!string.IsNullOrWhiteSpace(username) && password is not null)
        {
            SetBasicAuthentication(username, password);
        }

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public void SetBasicAuthentication(string username, string password)
    {
        string raw = $"{username}:{password}";
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
    }

    public Task<CapabilitiesResponse> GetCapabilitiesAsync(string metadataUuid, CancellationToken cancellationToken = default)
    {
        return GetAsync<CapabilitiesResponse>($"api/capabilities/{metadataUuid}", cancellationToken);
    }

    public Task<List<AreaOption>> GetAreasAsync(string metadataUuid, CancellationToken cancellationToken = default)
    {
        return GetAsync<List<AreaOption>>($"api/v2/codelists/area/{metadataUuid}", cancellationToken);
    }

    public Task<List<ProjectionOption>> GetProjectionsAsync(string metadataUuid, CancellationToken cancellationToken = default)
    {
        return GetAsync<List<ProjectionOption>>($"api/v2/codelists/projection/{metadataUuid}", cancellationToken);
    }

    public Task<List<FormatOption>> GetFormatsAsync(string metadataUuid, CancellationToken cancellationToken = default)
    {
        return GetAsync<List<FormatOption>>($"api/v2/codelists/format/{metadataUuid}", cancellationToken);
    }

    public Task<CanDownloadResponse> CanDownloadAsync(CanDownloadRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<CanDownloadRequest, CanDownloadResponse>("api/v2/can-download", request, cancellationToken);
    }

    public Task<OrderResponse> CreateOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        return PostAsync<OrderRequest, OrderResponse>("api/v2/order", request, cancellationToken);
    }

    public Task<OrderResponse> CreateOrderV3Async(OrderRequest request, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        return PostAsync<OrderRequest, OrderResponse>("api/order", request, cancellationToken, bearerToken);
    }

    public Task<OrderResponse> GetOrderAsync(string referenceNumber, CancellationToken cancellationToken = default)
    {
        return GetAsync<OrderResponse>($"api/v2/order/{referenceNumber}", cancellationToken);
    }

    public Task<OrderResponse> GetOrderV3Async(string referenceNumber, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        return GetAsync<OrderResponse>($"api/order/{referenceNumber}", cancellationToken, bearerToken);
    }

    public async Task DownloadOrderFileAsync(string referenceNumber, string fileId, string destinationPath, CancellationToken cancellationToken = default)
    {
        string relativeUrl = $"api/v2/download/order/{referenceNumber}/{fileId}";
        await DownloadAsync(relativeUrl, destinationPath, cancellationToken, null);
    }

    public async Task DownloadOrderFileV3Async(string referenceNumber, string fileId, string destinationPath, string? bearerToken = null, CancellationToken cancellationToken = default)
    {
        string relativeUrl = $"api/download/order/{referenceNumber}/{fileId}";
        await DownloadAsync(relativeUrl, destinationPath, cancellationToken, bearerToken);
    }

    public Task DownloadFromUrlAsync(string absoluteOrRelativeUrl, string destinationPath, CancellationToken cancellationToken = default)
    {
        return DownloadAsync(absoluteOrRelativeUrl, destinationPath, cancellationToken, null);
    }

    public Task DownloadFromUrlAsync(string absoluteOrRelativeUrl, string destinationPath, string? bearerToken, CancellationToken cancellationToken = default)
    {
        return DownloadAsync(absoluteOrRelativeUrl, destinationPath, cancellationToken, bearerToken);
    }

    public string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, _serializerOptions);
    }

    private async Task<TResponse> GetAsync<TResponse>(string relativeOrAbsoluteUrl, CancellationToken cancellationToken, string? bearerToken = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeOrAbsoluteUrl);
        AddBearerToken(request, bearerToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadResponse<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string relativeOrAbsoluteUrl, TRequest request, CancellationToken cancellationToken, string? bearerToken = null)
    {
        string json = JsonSerializer.Serialize(request, _serializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, relativeOrAbsoluteUrl)
        {
            Content = content
        };

        AddBearerToken(requestMessage, bearerToken);

        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        return await ReadResponse<TResponse>(response, cancellationToken);
    }

    private async Task<TResponse> ReadResponse<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GeoNorgeApiException(
                response.StatusCode,
                $"GeoNorge API request failed with {(int)response.StatusCode} {response.ReasonPhrase}",
                payload);
        }

        TResponse? value = JsonSerializer.Deserialize<TResponse>(payload, _serializerOptions);
        if (value is null)
        {
            throw new InvalidOperationException("GeoNorge API returned empty or invalid JSON response.");
        }

        return value;
    }

    private async Task DownloadAsync(string relativeOrAbsoluteUrl, string destinationPath, CancellationToken cancellationToken, string? bearerToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? ".");

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeOrAbsoluteUrl);
        AddBearerToken(request, bearerToken);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string payload = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GeoNorgeApiException(
                response.StatusCode,
                $"GeoNorge API download failed with {(int)response.StatusCode} {response.ReasonPhrase}",
                payload);
        }

        await using Stream networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = File.Create(destinationPath);
        await networkStream.CopyToAsync(fileStream, cancellationToken);
    }

    private static void AddBearerToken(HttpRequestMessage request, string? bearerToken)
    {
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }
    }
}
