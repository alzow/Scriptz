using QueueApp.Framework.Base;
using QueueApp.Services.Api;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Services.Api.ServiceOfferings;

public class ServiceOfferingsService : BaseService, IServiceOfferingsService
{
    private readonly IServiceOfferingsApi _api;

    public ServiceOfferingsService(IServiceOfferingsApi api)
    {
        _api = api;
    }

    public Task<List<ServiceResponse>> GetServicesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetServicesAsync(PostgrestFilter.Eq(businessId)));

    public Task<List<ServiceResponse>> GetActiveServicesAsync(Guid businessId) =>
        ExecuteApiCallAsync(_api.GetActiveServicesAsync(PostgrestFilter.Eq(businessId)));

    public Task<List<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request) =>
        ExecuteApiCallAsync(_api.CreateServiceAsync(request));

    public Task UpdateServiceAsync(Guid id, UpdateServiceRequest request) =>
        ExecuteApiCallAsync(_api.UpdateServiceAsync(PostgrestFilter.Eq(id), request));

    public Task SetServiceActiveAsync(Guid id, bool isActive) =>
        ExecuteApiCallAsync(_api.SetServiceActiveAsync(PostgrestFilter.Eq(id), new SetServiceActiveRequest { IsActive = isActive }));
}
