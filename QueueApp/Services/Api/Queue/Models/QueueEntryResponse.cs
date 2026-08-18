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

    [JsonIgnore] [ObservableProperty] private bool _isServing;
    [JsonIgnore] [ObservableProperty] private bool _isCompleting;
    [JsonIgnore] [ObservableProperty] private bool _isMarkingNoShow;
}
