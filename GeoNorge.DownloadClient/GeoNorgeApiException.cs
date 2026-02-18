using System.Net;

namespace GeoNorge.DownloadClient;

public sealed class GeoNorgeApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public GeoNorgeApiException(HttpStatusCode statusCode, string message, string responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
