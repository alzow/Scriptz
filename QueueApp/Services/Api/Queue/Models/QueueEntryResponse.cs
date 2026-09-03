using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QueueApp.Services.Api.Queue.Models;

public partial class QueueEntryResponse : ObservableObject
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("business_id")] public Guid BusinessId { get; set; }
    [JsonPropertyName("operator_id")] public Guid? OperatorId { get; set; }
    [JsonPropertyName("service_id")] public Guid? ServiceId { get; set; }
    [JsonPropertyName("customer_id")] public Guid? CustomerId { get; set; }
    [JsonPropertyName("customer_name")] public string? CustomerName { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "waiting";
    [JsonPropertyName("joined_at")] public DateTime JoinedAt { get; set; }

    // Set by start_serving. The operator board counts the elapsed timer up from here, so a card
    // that outlives a page reload still shows the true elapsed time rather than restarting at 0.
    [JsonPropertyName("serving_at")] public DateTime? ServingAt { get; set; }
    [JsonPropertyName("done_at")] public DateTime? DoneAt { get; set; }

    [JsonPropertyName("awaiting_collection_at")] public DateTime? AwaitingCollectionAt { get; set; }
    [JsonPropertyName("collected_at")] public DateTime? CollectedAt { get; set; }

    [JsonPropertyName("progress_status")] public string? ProgressStatus { get; set; }

    // Carries join_queue's assignment stamp onto the board, so a card can say whether the shop's
    // own rules put this person in this chair or the customer asked for it by name.
    [JsonPropertyName("details")] public QueueEntryDetails? Details { get; set; }

    [JsonIgnore] public bool HasProgress => !string.IsNullOrWhiteSpace(ProgressStatus);
    [JsonIgnore] public bool WasAutoAssigned => Details?.WasAutoAssigned == true;
    [JsonIgnore] public bool IsAwaitingCollection => Status == QueueEntryStatuses.AwaitingCollection;

    [JsonIgnore] [ObservableProperty] private bool _isServing;
    [JsonIgnore] [ObservableProperty] private bool _isCompleting;
    [JsonIgnore] [ObservableProperty] private bool _isMarkingNoShow;
    [JsonIgnore] [ObservableProperty] private bool _isSavingProgress;
}
