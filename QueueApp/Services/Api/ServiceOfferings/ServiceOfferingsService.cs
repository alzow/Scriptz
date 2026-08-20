using QueueApp.Framework.Base;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Services.Api.ServiceOfferings;

// Hides PostgREST filter syntax (e.g. "eq.<guid>") from callers.
public class ServiceOfferingsService : BaseService, IServiceOfferingsService
{
    private readonly IServiceOfferingsApi _api;

    public ServiceOfferingsService(IServiceOfferingsApi api)
    {
        _api = api;
    }

    public Task<List<ServiceResponse>> GetServicesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetServicesAsync($"eq.{businessId}"));

    public Task<List<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request) =>
        ExecuteApiCallAsync(_api.CreateServiceAsync(request));

    public Task UpdateServiceAsync(Guid id, UpdateServiceRequest request) =>
        ExecuteApiCallAsync(_api.UpdateServiceAsync($"eq.{id}", request));

    public Task SetServiceActiveAsync(Guid id, bool isActive) =>
        ExecuteApiCallAsync(_api.SetServiceActiveAsync($"eq.{id}", new SetServiceActiveRequest { IsActive = isActive }));
}
