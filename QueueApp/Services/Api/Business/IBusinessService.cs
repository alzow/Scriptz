using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Api.Business;

public interface IBusinessService
{
    Task<Guid> GetOwnedBusinessIdAsync();
    Task<BusinessResponse?> GetBusinessAsync(Guid businessId);
    Task<List<BusinessResponse>> GetBusinessesAsync(string category, string suburb = "Lenasia");
    Task HeartbeatAsync(Guid businessId);
}
