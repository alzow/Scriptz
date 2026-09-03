using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using QueueApp.Constants;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api.Intake;

public class IntakeFileService : IIntakeFileService
{
    private readonly IAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly FilePickerFileType ImagesAndPdf = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] = new[] { "image/*", "application/pdf" },
            [DevicePlatform.iOS] = new[] { "public.image", "com.adobe.pdf" },
            [DevicePlatform.MacCatalyst] = new[] { "public.image", "com.adobe.pdf" },
            [DevicePlatform.WinUI] = new[] { ".png", ".jpg", ".jpeg", ".heic", ".pdf" },
        });

    public IntakeFileService(IAuthService authService, IHttpClientFactory httpClientFactory)
    {
        _authService = authService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IntakeFileRef?> PickAndUploadAsync(Guid serviceId, Guid fieldId)
    {
        var userId = await _authService.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("No authenticated user is available for intake file upload.");

        var picked = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose an image or PDF",
            FileTypes = ImagesAndPdf,
        });

        if (picked is null)
            return null;

        var objectKey = $"{userId}/{serviceId}/{fieldId}/{Guid.NewGuid()}{Path.GetExtension(picked.FileName)}";
        var objectPath = BuildObjectPath(objectKey);
        var contentType = string.IsNullOrWhiteSpace(picked.ContentType)
            ? "application/octet-stream"
            : picked.ContentType;

        await using var source = await picked.OpenReadAsync();
        using var content = new StreamContent(source);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"storage/v1/object/{SupabaseConfig.IntakeUploadsBucket}/{objectPath}")
        {
            Content = content,
        };
        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        using var response = await _httpClientFactory
            .CreateClient(RefitConfiguration.SupabaseStorageClientName)
            .SendAsync(request);
        response.EnsureSuccessStatusCode();

        return new IntakeFileRef
        {
            Path = objectKey,
            Name = picked.FileName,
            ContentType = contentType,
            SizeBytes = SizeOf(picked),
        };
    }

    public async Task<string> DownloadAsync(IntakeFileRef file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (string.IsNullOrWhiteSpace(file.Path))
            throw new InvalidOperationException("The intake file has no storage path.");

        var objectPath = BuildObjectPath(file.Path);

        var safeFileName = Path.GetFileName(file.Name);
        if (string.IsNullOrWhiteSpace(safeFileName))
            safeFileName = "intake-file";

        var localPath = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}-{safeFileName}");
        using var response = await _httpClientFactory
            .CreateClient(RefitConfiguration.SupabaseStorageClientName)
            .GetAsync($"storage/v1/object/authenticated/{SupabaseConfig.IntakeUploadsBucket}/{objectPath}",
                HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var destination = File.Create(localPath);
        await source.CopyToAsync(destination);

        return localPath;
    }

    private static string BuildObjectPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            throw new InvalidOperationException("The intake file has an invalid storage path.");

        // Stored references created before the key convention changed include the bucket name.
        if (string.Equals(segments[0], SupabaseConfig.IntakeUploadsBucket, StringComparison.Ordinal))
            segments = segments[1..];

        if (segments.Length == 0)
            throw new InvalidOperationException("The intake file has an invalid storage path.");

        return string.Join('/', segments.Select(Uri.EscapeDataString));
    }

    private static long? SizeOf(FileResult picked)
    {
        try
        {
            return new FileInfo(picked.FullPath).Length;
        }
        catch (Exception ex)
        {
            // A size the step can't show is worth nothing and worth failing nothing over.
            Debug.WriteLine($"Could not size the picked intake file: {ex.Message}");
            return null;
        }
    }
}
