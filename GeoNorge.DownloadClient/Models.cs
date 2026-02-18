using System.Text.Json.Serialization;

namespace GeoNorge.DownloadClient;

public static class GeoNorgeRel
{
    public const string Projection = "http://rel.geonorge.no/download/projection";
    public const string Format = "http://rel.geonorge.no/download/format";
    public const string Area = "http://rel.geonorge.no/download/area";
    public const string Order = "http://rel.geonorge.no/download/order";
    public const string CanDownload = "http://rel.geonorge.no/download/can-download";
}

public sealed class ApiLink
{
    [JsonPropertyName("href")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("rel")]
    public string Rel { get; set; } = string.Empty;

    [JsonPropertyName("templatedSpecified")]
    public bool TemplatedSpecified { get; set; }
}

public sealed class CapabilitiesResponse
{
    [JsonPropertyName("supportsProjectionSelection")]
    public bool SupportsProjectionSelection { get; set; }

    [JsonPropertyName("supportsFormatSelection")]
    public bool SupportsFormatSelection { get; set; }

    [JsonPropertyName("supportsPolygonSelection")]
    public bool SupportsPolygonSelection { get; set; }

    [JsonPropertyName("supportsAreaSelection")]
    public bool SupportsAreaSelection { get; set; }

    [JsonPropertyName("mapSelectionLayer")]
    public string? MapSelectionLayer { get; set; }

    [JsonPropertyName("_links")]
    public List<ApiLink> Links { get; set; } = new();
}

public sealed class ProjectionOption
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("codespace")]
    public string? Codespace { get; set; }
}

public sealed class FormatOption
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("projections")]
    public List<ProjectionOption>? Projections { get; set; }
}

public sealed class AreaOption
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("projections")]
    public List<ProjectionOption> Projections { get; set; } = new();

    [JsonPropertyName("formats")]
    public List<FormatOption> Formats { get; set; } = new();
}

public sealed class CanDownloadRequest
{
    [JsonPropertyName("metadataUuid")]
    public string MetadataUuid { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public string Coordinates { get; set; } = string.Empty;

    [JsonPropertyName("coordinateSystem")]
    public string CoordinateSystem { get; set; } = string.Empty;
}

public sealed class CanDownloadResponse
{
    [JsonPropertyName("canDownload")]
    public bool CanDownload { get; set; }
}

public sealed class AreaSelection
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class OrderLineRequest
{
    [JsonPropertyName("metadataUuid")]
    public string MetadataUuid { get; set; } = string.Empty;

    [JsonPropertyName("areas")]
    public List<AreaSelection> Areas { get; set; } = new();

    [JsonPropertyName("projections")]
    public List<ProjectionOption> Projections { get; set; } = new();

    [JsonPropertyName("formats")]
    public List<FormatOption> Formats { get; set; } = new();

    [JsonPropertyName("coordinates")]
    public string? Coordinates { get; set; }

    [JsonPropertyName("usagePurpose")]
    public List<string>? UsagePurpose { get; set; }
}

public sealed class OrderRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("usageGroup")]
    public string? UsageGroup { get; set; }

    [JsonPropertyName("softwareClient")]
    public string? SoftwareClient { get; set; }

    [JsonPropertyName("softwareClientVersion")]
    public string? SoftwareClientVersion { get; set; }

    [JsonPropertyName("orderLines")]
    public List<OrderLineRequest> OrderLines { get; set; } = new();
}

public sealed class OrderFile
{
    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = string.Empty;

    [JsonPropertyName("metadataUuid")]
    public string? MetadataUuid { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("areaName")]
    public string? AreaName { get; set; }

    [JsonPropertyName("projection")]
    public string? Projection { get; set; }

    [JsonPropertyName("projectionName")]
    public string? ProjectionName { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("metadataName")]
    public string? MetadataName { get; set; }

    [JsonPropertyName("coordinates")]
    public string? Coordinates { get; set; }
}

public sealed class OrderResponse
{
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<OrderFile> Files { get; set; } = new();

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("orderDate")]
    public DateTimeOffset? OrderDate { get; set; }
}
