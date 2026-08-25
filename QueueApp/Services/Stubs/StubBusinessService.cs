using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Business.Models;

namespace QueueApp.Services.Stubs;

// In-memory stub so the Queue screen can be fully tested without a Supabase project.
// Registered instead of the real BusinessService in DEBUG builds.
public class StubBusinessService : IBusinessService
{
    private readonly Guid _defaultBusinessId = new("0637f5ef-c7fa-46dc-b4e5-b814f2d7d3bf");

    // A second, fixed-id business so the "allow_operator_choice = false" (pooled) customer-facing
    // flow — no picker, one Join button — is reachable and testable in USE_STUBS builds without a
    // live Supabase project. Matches "Trim & Fade" in GetBrowseBusinessesAsync below.
    //
    // Known stub-fidelity gap: StubOperatorService's operator roster isn't business-scoped (a
    // pre-existing simplification — it always returns the same "Ahmed"/"Yusuf" pair regardless of
    // business id), and StubQueueService.StartServingAsync doesn't simulate 17a's real
    // resource-assignment-on-Serve logic. So this covers the customer-facing join flow
    // (17b.2/17b.4) end to end, but not the Manage-side "assign to a free bay" nuance (17b.3) —
    // that needs the real backend once STEP-17-SUPABASE.md's SQL is applied.
    private static readonly Guid PooledBusinessId = new("7c9e5a2b-3f6d-4e1a-8b2c-5d9f0a1e6c47");

    public Task<Guid> GetOwnedBusinessIdAsync() => Task.FromResult(_defaultBusinessId);

    public Task<BusinessResponse?> GetBusinessAsync(Guid businessId)
        => Task.FromResult<BusinessResponse?>(businessId == PooledBusinessId
            ? new BusinessResponse
              {
                  Id = businessId,
                  Name = "Trim & Fade",
                  Category = "barber",
                  Mode = "queue",
                  Suburb = "Lenasia",
                  Address = "Main Rd, Lenasia",
                  IsActive = true,
                  LastSeenAt = DateTime.UtcNow,
                  AllowOperatorChoice = false,
              }
            : new BusinessResponse
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

    public Task UpdateLocationAsync(Guid businessId, double latitude, double longitude) => Task.CompletedTask;

    public Task<List<BrowseBusinessSummaryResponse>> GetBrowseBusinessesAsync(
        string? category, string suburb = "Lenasia", double? customerLatitude = null, double? customerLongitude = null)
    {
        var businesses = new List<BrowseBusinessSummaryResponse>
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
                DistanceKm = 1.2, // fallback shown until a customer location is known
                IsActive = true,
                LastSeenAt = DateTime.UtcNow,
                WaitingCount = 2,
                OperatorsWorkingCount = 3,
                AvgWaitMinutes = 8,
            },
            new()
            {
                Id = PooledBusinessId,
                Name = "Trim & Fade",
                Category = "barber",
                Mode = "queue",
                Address = "Main Rd, Lenasia",
                Latitude = -26.3200,
                Longitude = 27.8450,
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
                Latitude = -26.3050,
                Longitude = 27.8600,
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
                Latitude = -26.3180,
                Longitude = 27.8520,
                DistanceKm = 1.1,
                IsActive = true,
                LastSeenAt = DateTime.UtcNow.AddHours(-12), // outside the 15-min presence window -> closed
            },
        };

        // Once a customer location is known, replace the placeholder distances with real ones —
        // mirrors what nearby_business_summary does server-side, so the sort order is demoable.
        if (customerLatitude.HasValue && customerLongitude.HasValue)
        {
            foreach (var b in businesses)
            {
                if (b.Latitude.HasValue && b.Longitude.HasValue)
                {
                    b.DistanceKm = HaversineKm(
                        customerLatitude.Value, customerLongitude.Value, b.Latitude.Value, b.Longitude.Value);
                }
            }
        }

        return Task.FromResult(businesses);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = double.DegreesToRadians(lat2 - lat1);
        var dLon = double.DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(double.DegreesToRadians(lat1)) * Math.Cos(double.DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }
}
