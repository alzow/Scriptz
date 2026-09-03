using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Intake;

public interface IIntakeFieldsService
{
    // Fails soft: the join flow calls this on every load, and a business with no intake fields —
    // which is every business today — must not lose its join flow to a table that isn't there yet.
    Task<Dictionary<Guid, List<IntakeFieldResponse>>> GetFieldsByServiceAsync(Guid businessId);

    Task<List<IntakeFieldResponse>> GetFieldsForServiceAsync(Guid serviceId);
    Task<IntakeFieldResponse?> CreateFieldAsync(CreateIntakeFieldRequest request);
    Task UpdateFieldAsync(Guid fieldId, UpdateIntakeFieldRequest request);
    Task SetFieldOrderAsync(Guid fieldId, int sortOrder);
    Task DeleteFieldAsync(Guid fieldId);
}
