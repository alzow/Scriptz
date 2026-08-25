using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Services.Api.Booking.Models;

public class AgendaCustomerRef
{
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
}

public partial class AgendaBookingResponse : ObservableObject
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("starts_at")] public DateTimeOffset StartsAt { get; set; }
    [JsonPropertyName("ends_at")] public DateTimeOffset EndsAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("operator")] public VisitOperatorRef? Operator { get; set; }
    [JsonPropertyName("service")] public VisitServiceRef? Service { get; set; }
    [JsonPropertyName("customer")] public AgendaCustomerRef? Customer { get; set; }
    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    [JsonIgnore] [ObservableProperty] private bool _isConfirming;
    [JsonIgnore] [ObservableProperty] private bool _isCompleting;
    [JsonIgnore] [ObservableProperty] private bool _isCancelling;
    [JsonIgnore] [ObservableProperty] private bool _isSavingProgress;

    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);

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
