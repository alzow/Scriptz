using System.Text.Json.Serialization;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Booking.Models;

public class AgendaCustomerRef
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
}

public class AgendaBookingResponse
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }
    [JsonPropertyName("customer")] public AgendaCustomerRef? Customer { get; set; }

    [JsonIgnore] public bool IsConfirming { get; set; }
    [JsonIgnore] public bool IsCompleting { get; set; }
    [JsonIgnore] public bool IsCancelling { get; set; }

    [JsonIgnore] public string OperatorName => Operator?.DisplayName ?? "Any available";
    [JsonIgnore] public string ServiceName => Service?.Name ?? "";
    [JsonIgnore] public string CustomerName => Customer?.DisplayName ?? "Customer";

    [JsonIgnore]
    private DateTimeOffset LocalStart => StartsAt.ToOffset(TimeSpan.FromHours(2));
    [JsonIgnore]
    private DateTimeOffset LocalEnd => EndsAt.ToOffset(TimeSpan.FromHours(2));
    [JsonIgnore]
    public string TimeRangeDisplay => $"{LocalStart:h:mm tt} - {LocalEnd:h:mm tt}";

    [JsonIgnore] public bool CanConfirm => Status == "pending";
    [JsonIgnore] public bool CanComplete => Status is "pending" or "confirmed";
    [JsonIgnore] public bool CanCancel => Status is "pending" or "confirmed";

    [JsonIgnore]
    public string StatusLabel => Status switch
    {
        "pending" => "Pending",
        "confirmed" => "Confirmed",
        "cancelled" => "Cancelled",
        "completed" => "Completed",
        _ => Status
    };
}
