using System.Diagnostics;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Auth;

namespace QueueApp.Services.Api.Intake;

// Picking is a device capability and is real. Uploading is not: file-upload fields need a Supabase
// Storage bucket that does not exist, and inventing a bucket name and an access policy here would
// be guessing at the two decisions that matter most.
//
// So the pick is genuine — the customer chooses a real script or photo, and the step shows its real
// name and size — and what comes back is a path into a bucket nothing has created yet. That is
// enough to exercise the whole flow, and it is deliberately not enough to ship.
//
// TODO: stub — see the "File storage" section of
// Documentation/service-intake-fields-backend-requirements.md.
public class IntakeFileService : IIntakeFileService
{
    private readonly IAuthService _authService;

    private static readonly FilePickerFileType ImagesAndPdf = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] = new[] { "image/*", "application/pdf" },
            [DevicePlatform.iOS] = new[] { "public.image", "com.adobe.pdf" },
            [DevicePlatform.MacCatalyst] = new[] { "public.image", "com.adobe.pdf" },
            [DevicePlatform.WinUI] = new[] { ".png", ".jpg", ".jpeg", ".heic", ".pdf" },
        });

    public IntakeFileService(IAuthService authService)
    {
        _authService = authService;
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

        return new IntakeFileRef
        {
            // The path the real upload would write to, so the shape stored in intake_responses is
            // already the shape the backend will have to serve.
            Path = $"{userId}/{serviceId}/{fieldId}/{Guid.NewGuid()}{System.IO.Path.GetExtension(picked.FileName)}",
            Name = picked.FileName,
            ContentType = picked.ContentType,
            SizeBytes = SizeOf(picked),
        };
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
