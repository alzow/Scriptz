using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Services.Api.ServiceOfferings;

public interface IServiceOfferingsService
{
    Task<List<ServiceResponse>> GetServicesAsync(Guid businessId);
    Task<List<ServiceResponse>> GetActiveServicesAsync(Guid businessId);
    Task<List<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request);
    Task UpdateServiceAsync(Guid id, UpdateServiceRequest request);
    Task SetServiceActiveAsync(Guid id, bool isActive);
}
