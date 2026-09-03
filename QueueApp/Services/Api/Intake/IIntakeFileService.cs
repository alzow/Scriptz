using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Intake;

public interface IIntakeFileService
{
    // Null when the customer backed out of the picker. Throws only when the pick or the upload
    // actually failed, so the step can say so.
    Task<IntakeFileRef?> PickAndUploadAsync(Guid serviceId, Guid fieldId);
}
