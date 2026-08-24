using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real BusinessService in DEBUG builds.
public class StubBusinessService : IBusinessService
{
    private readonly Guid _defaultBusinessId = new("0637f5ef-c7fa-46dc-b4e5-b814f2d7d3bf");

    public Task<Guid> GetOwnedBusinessIdAsync() => Task.FromResult(_defaultBusinessId);

    public Task<BusinessResponse?> GetBusinessAsync(Guid businessId)
        => Task.FromResult<BusinessResponse?>(new BusinessResponse
        {
            Id = businessId,
            Name = "My Test Barber",
            Category = "barber",
            Mode = "queue",
            Suburb = "Lenasia",
            Address = "123 Test Street",
            IsActive = true,
            LastSeenAt = DateTime.UtcNow,
        });

    public Task<List<BusinessResponse>> GetBusinessesAsync(string category, string suburb = "Lenasia")
        => Task.FromResult(new List<BusinessResponse>
        {
            new()
            {
                Id = _defaultBusinessId,
                Name = "My Test Barber",
                Category = category,
                Mode = "queue",
                Suburb = suburb,
                Address = "123 Test Street",
                IsActive = true,
                LastSeenAt = DateTime.UtcNow,
            },
        });

    public Task HeartbeatAsync(Guid businessId) => Task.CompletedTask;

    public Task<List<BrowseBusinessSummaryResponse>> GetBrowseBusinessesAsync(string? category, string suburb = "Lenasia")
        => Task.FromResult(new List<BrowseBusinessSummaryResponse>
        {
            new()
            {
                Id = _defaultBusinessId,
                Name = "Nu-Look Barbers",
                Category = "barber",
                Mode = "queue",
                Address = "Rose Ave, Lenasia",
                Latitude = -26.3167,
                Longitude = 27.8500,
                DistanceKm = 1.2,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow,
                WaitingCount = 2,
                OperatorsWorkingCount = 3,
                AvgWaitMinutes = 8,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Trim & Fade",
                Category = "barber",
                Mode = "queue",
                Address = "Main Rd, Lenasia",
                DistanceKm = 0.8,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow,
                WaitingCount = 9,
                OperatorsWorkingCount = 1,
                AvgWaitMinutes = 41,
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Precision Auto Care",
                Category = "barber",
                Mode = "booking",
                Address = "Extension 2, Lenasia",
                DistanceKm = 3.1,
                IsActive = true,
                NextSlotStartsAt = DateTimeOffset.UtcNow.Date.AddHours(15).AddMinutes(30).AddHours(-2),
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Laxmi Pharmacy",
                Category = "barber",
                Mode = "queue",
                Address = "Extension 2, Lenasia",
                DistanceKm = 1.1,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow.AddHours(-12), // outside the 15-min presence window -> closed
            },
        });
}
