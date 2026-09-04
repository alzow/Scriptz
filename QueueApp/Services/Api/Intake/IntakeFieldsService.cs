using System.Diagnostics;
using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Services.Api.Intake;

public class IntakeFieldsService : BaseService, IIntakeFieldsService
{
    private readonly IIntakeFieldsApi _api;

    public IntakeFieldsService(IIntakeFieldsApi api)
    {
        _api = api;
    }

    // The one read the join flow makes, and the only one that swallows its failure. Every business
    // that exists today has no intake fields at all, so "the table isn't there" and "this business
    // asks nothing" have to land on the same answer: an empty map, and a join flow that behaves
    // exactly as it did before this feature existed.
    //
    // TODO: stub — drop the catch once service_intake_fields exists and this read is guaranteed.
    public async Task<Dictionary<Guid, List<IntakeFieldResponse>>> GetFieldsByServiceAsync(Guid businessId)
    {
        try
        {
            var fields = await ExecuteApiCallAsync(_api.GetFieldsForBusinessAsync(PostgrestFilter.Eq(businessId)));
            return Group(fields);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not read intake fields for {businessId}: {ex.Message}");
            return new Dictionary<Guid, List<IntakeFieldResponse>>();
        }
    }

    public Task<List<IntakeFieldResponse>> GetFieldsForServiceAsync(Guid serviceId) =>
        ExecuteApiCallAsync(_api.GetFieldsForServiceAsync(PostgrestFilter.Eq(serviceId)));

    public Task<IntakeFieldResponse?> CreateFieldAsync(CreateIntakeFieldRequest request) =>
        ExecuteSingleAsync(_api.CreateFieldAsync(request));

    public Task UpdateFieldAsync(Guid fieldId, UpdateIntakeFieldRequest request) =>
        ExecuteApiCallAsync(_api.UpdateFieldAsync(PostgrestFilter.Eq(fieldId), request));

    public Task SetFieldOrderAsync(Guid fieldId, int sortOrder) =>
        ExecuteApiCallAsync(_api.SetFieldOrderAsync(PostgrestFilter.Eq(fieldId),
            new SetIntakeFieldOrderRequest { SortOrder = sortOrder }));

    public Task DeleteFieldAsync(Guid fieldId) =>
        ExecuteApiCallAsync(_api.DeleteFieldAsync(PostgrestFilter.Eq(fieldId)));

    public static Dictionary<Guid, List<IntakeFieldResponse>> Group(IEnumerable<IntakeFieldResponse> fields) =>
        fields
            .GroupBy(f => f.ServiceId)
            .ToDictionary(g => g.Key, g => g.OrderBy(f => f.SortOrder).ToList());
}
