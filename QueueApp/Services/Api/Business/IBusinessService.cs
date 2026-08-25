using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Api.Business;

public interface IBusinessService
{
    Task<Guid> GetOwnedBusinessIdAsync();
    Task<BusinessResponse?> GetBusinessAsync(Guid businessId);
    Task<List<BusinessResponse>> GetBusinessesAsync(string category, string suburb = "Lenasia");
    Task<List<BrowseBusinessSummaryResponse>> GetBrowseBusinessesAsync(
        string? category, string suburb = "Lenasia", double? customerLatitude = null, double? customerLongitude = null);
    Task HeartbeatAsync(Guid businessId);
    Task UpdateLocationAsync(Guid businessId, double latitude, double longitude);
}
