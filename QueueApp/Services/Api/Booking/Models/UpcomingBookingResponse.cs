using System.Text.Json.Serialization;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Booking.Models;

public class UpcomingBookingBusinessRef
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class UpcomingBookingResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("business")] public UpcomingBookingBusinessRef? Business { get; set; }
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }

    [JsonIgnore] public bool IsCancelling { get; set; }

    [JsonIgnore] public string BusinessName => Business?.Name ?? "";
    [JsonIgnore] public string OperatorName => Operator?.DisplayName ?? "Any available";
    [JsonIgnore] public string ServiceName => Service?.Name ?? "";

    [JsonIgnore]
    private DateTimeOffset LocalStart => StartsAt.ToOffset(TimeSpan.FromHours(2));

    [JsonIgnore]
    public string DateTimeDisplay => LocalStart.ToString("ddd d MMM, h:mm tt");

    [JsonIgnore] public string DayText => LocalStart.ToString("d");
    [JsonIgnore] public string MonthText => LocalStart.ToString("MMM").ToUpperInvariant();
    [JsonIgnore] public string TimeText => LocalStart.ToString("h:mm tt");

    [JsonIgnore] public bool IsCancellable => Status is "pending" or "confirmed";
    [JsonIgnore] public string StatusLabel => Status == "pending" ? "Pending" : "Confirmed";
}
