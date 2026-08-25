using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace QueueApp.Services.Api;

// Logs method, URI, status code, duration, and body (for text-based content types) for every
// request that passes through the Refit handler pipeline. Useful for debugging API calls.
public class HttpLoggingHandler : DelegatingHandler
{
    private static readonly string[] TextContentTypes =
        { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        var requestBody = request.Content != null && IsTextBasedContentType(request.Content.Headers)
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var responseBody = response.Content != null && IsTextBasedContentType(response.Content.Headers)
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : null;

        LogHttpMessage(id, request, response, requestBody, responseBody, stopwatch.Elapsed);

        return response;
    }

    private void LogHttpMessage(
        string id,
        HttpRequestMessage request,
        HttpResponseMessage response,
        string? requestBody,
        string? responseBody,
        TimeSpan duration)
    {
        var log = GenerateDebugLog(id, request, response, requestBody, responseBody, duration);

        Debug.WriteLine(log);
        _logger.LogInformation("{HttpLog}", log);
    }

    private static string GenerateDebugLog(
        string id,
        HttpRequestMessage request,
        HttpResponseMessage response,
        string? requestBody,
        string? responseBody,
        TimeSpan duration)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"[{id}]======== Start ==========");
        sb.AppendLine($"[{id}] {request.Method} {request.RequestUri} HTTP/{response.Version}");
        sb.AppendLine($"[{id}] Status: {(int)response.StatusCode} {response.ReasonPhrase}");

        sb.AppendLine($"[{id}] Headers:");
        AppendHeaders(sb, id, response.Headers);

        if (response.Content?.Headers != null)
        {
            sb.AppendLine($"[{id}] Content Headers:");
            AppendHeaders(sb, id, response.Content.Headers);
        }

        if (!string.IsNullOrEmpty(requestBody))
            sb.AppendLine($"[{id}] Request Data: {Truncate(requestBody, 255)}");

        if (!string.IsNullOrEmpty(responseBody))
            sb.AppendLine($"[{id}] Response Data: {Truncate(responseBody, 255)}");

        sb.AppendLine($"[{id}] Duration: {duration.TotalMilliseconds} ms");
        sb.AppendLine($"[{id}]========= End ==========");

        return sb.ToString();
    }

    private static void AppendHeaders(StringBuilder sb, string id, HttpHeaders headers)
    {
        foreach (var header in headers)
            sb.AppendLine($"[{id}] {header.Key}: {string.Join(", ", header.Value)}");
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...");
    }

    private static bool IsTextBasedContentType(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Content-Type", out var values))
            return false;

        var header = string.Join(" ", values).ToLowerInvariant();
        return TextContentTypes.Any(t => header.Contains(t));
    }
}
