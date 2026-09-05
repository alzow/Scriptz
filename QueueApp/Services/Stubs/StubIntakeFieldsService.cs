using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Intake.Models;
using QueueApp.Services.Api.ServiceOfferings;

namespace QueueApp.Services.Stubs;

// In-memory stand-in for service_intake_fields so both halves of this feature — the customer's
// intake step and the owner's editor — can be exercised before the table exists.
//
// The seed hangs off the first service the stub offerings service reports for a business, and only
// that one. The second service stays empty on purpose: the whole point of the design is that a
// service with no fields walks the flow it walked before, and that is only worth anything if it can
// be seen side by side with one that asks.
public class StubIntakeFieldsService : IIntakeFieldsService
{
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly Dictionary<Guid, List<IntakeFieldResponse>> _fieldsByService = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _serviceIdsByBusiness = new();

    public StubIntakeFieldsService(IServiceOfferingsService serviceOfferingsService)
    {
        _serviceOfferingsService = serviceOfferingsService;
    }

    public async Task<Dictionary<Guid, List<IntakeFieldResponse>>> GetFieldsByServiceAsync(Guid businessId)
    {
        await EnsureSeededAsync(businessId);

        var serviceIds = _serviceIdsByBusiness.TryGetValue(businessId, out var ids)
            ? ids
            : new HashSet<Guid>();

        return _fieldsByService
            .Where(pair => serviceIds.Contains(pair.Key) && pair.Value.Count > 0)
            .ToDictionary(pair => pair.Key, pair => Ordered(pair.Value));
    }

    public Task<List<IntakeFieldResponse>> GetFieldsForServiceAsync(Guid serviceId) =>
        Task.FromResult(_fieldsByService.TryGetValue(serviceId, out var fields)
            ? Ordered(fields)
            : new List<IntakeFieldResponse>());

    public Task<IntakeFieldResponse?> CreateFieldAsync(CreateIntakeFieldRequest request)
    {
        var list = ListFor(request.ServiceId);

        var created = new IntakeFieldResponse
        {
            Id = Guid.NewGuid(),
            ServiceId = request.ServiceId,
            FieldType = request.FieldType,
            Label = request.Label,
            Hint = request.Hint,
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder,
            Options = request.Options,
            VisibilityRule = request.VisibilityRule,
        };

        list.Add(created);
        return Task.FromResult<IntakeFieldResponse?>(created);
    }

    public Task UpdateFieldAsync(Guid fieldId, UpdateIntakeFieldRequest request)
    {
        var field = Find(fieldId);
        if (field is not null)
        {
            field.FieldType = request.FieldType;
            field.Label = request.Label;
            field.Hint = request.Hint;
            field.IsRequired = request.IsRequired;
            field.Options = request.Options;
            field.VisibilityRule = request.VisibilityRule;
        }

        return Task.CompletedTask;
    }

    public Task SetFieldOrderAsync(Guid fieldId, int sortOrder)
    {
        var field = Find(fieldId);
        if (field is not null)
            field.SortOrder = sortOrder;

        return Task.CompletedTask;
    }

    public Task DeleteFieldAsync(Guid fieldId)
    {
        foreach (var list in _fieldsByService.Values)
            list.RemoveAll(f => f.Id == fieldId);

        return Task.CompletedTask;
    }

    private async Task EnsureSeededAsync(Guid businessId)
    {
        if (_serviceIdsByBusiness.ContainsKey(businessId))
            return;

        var services = await _serviceOfferingsService.GetActiveServicesAsync(businessId);
        _serviceIdsByBusiness[businessId] = services.Select(s => s.Id).ToHashSet();

        var first = services.FirstOrDefault();
        if (first is null)
            return;

        // One of each type, and a mix of required and optional, so the step's blocking rule and its
        // empty-answer rendering are both visible from the first run.
        _fieldsByService[first.Id] = new List<IntakeFieldResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ServiceId = first.Id,
                FieldType = IntakeFieldTypes.ShortText,
                Label = "What's the main thing you're coming in for?",
                IsRequired = true,
                SortOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ServiceId = first.Id,
                FieldType = IntakeFieldTypes.SingleSelect,
                Label = "Have you been in before?",
                IsRequired = true,
                SortOrder = 1,
                Options = new List<string> { "First time", "Been in before" },
            },
            new()
            {
                Id = Guid.NewGuid(),
                ServiceId = first.Id,
                FieldType = IntakeFieldTypes.MultiSelect,
                Label = "Anything we should be careful of?",
                IsRequired = false,
                SortOrder = 2,
                Options = new List<string> { "Allergies", "Skin sensitivity", "On medication" },
            },
            new()
            {
                Id = Guid.NewGuid(),
                ServiceId = first.Id,
                FieldType = IntakeFieldTypes.File,
                Label = "Script, referral or a photo — if you have one",
                IsRequired = false,
                SortOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                ServiceId = first.Id,
                FieldType = IntakeFieldTypes.LongText,
                Label = "Anything else worth knowing?",
                IsRequired = false,
                SortOrder = 4,
            },
        };
    }

    private List<IntakeFieldResponse> ListFor(Guid serviceId)
    {
        if (!_fieldsByService.TryGetValue(serviceId, out var list))
        {
            list = new List<IntakeFieldResponse>();
            _fieldsByService[serviceId] = list;
        }

        return list;
    }

    private IntakeFieldResponse? Find(Guid fieldId) =>
        _fieldsByService.Values.SelectMany(f => f).FirstOrDefault(f => f.Id == fieldId);

    private static List<IntakeFieldResponse> Ordered(IEnumerable<IntakeFieldResponse> fields) =>
        fields.OrderBy(f => f.SortOrder).ToList();
}
