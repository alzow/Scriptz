namespace QueueApp.Services.Intake;

public static class IntakeFileService
{
    public static string BuildStoragePath(
        string authUid,
        Guid serviceId,
        Guid fieldId,
        string fileName)
        => $"{authUid}/{serviceId}/{fieldId}/{fileName}";
}
