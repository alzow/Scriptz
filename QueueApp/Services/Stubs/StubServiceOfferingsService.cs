using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Services management screen can be fully tested without a Supabase project.
// Registered instead of the real ServiceOfferingsService in DEBUG builds.
public class StubServiceOfferingsService : IServiceOfferingsService
{
    private readonly List<ServiceResponse> _services = new();

    public Task<List<ServiceResponse>> GetServicesAsync(Guid businessId)
        => Task.FromResult(_services.Where(s => s.BusinessId == businessId).OrderBy(s => s.SortOrder).ToList());

    public Task<List<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request)
    {
        var service = new ServiceResponse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Name = request.Name,
            PriceCents = request.PriceCents,
            EstMinutes = request.EstMinutes,
            IsActive = true,
            SortOrder = _services.Count,
        };
        _services.Add(service);
        return Task.FromResult(new List<ServiceResponse> { service });
    }

    public Task UpdateServiceAsync(Guid id, UpdateServiceRequest request)
    {
        var service = _services.FirstOrDefault(s => s.Id == id);
        if (service != null)
        {
            service.Name = request.Name;
            service.PriceCents = request.PriceCents;
            service.EstMinutes = request.EstMinutes;
        }
        return Task.CompletedTask;
    }

    public Task SetServiceActiveAsync(Guid id, bool isActive)
    {
        var service = _services.FirstOrDefault(s => s.Id == id);
        if (service != null) service.IsActive = isActive;
        return Task.CompletedTask;
    }
}
