using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Intake.Models;

public class SetIntakeFieldOrderRequest
{
    [JsonPropertyName("sort_order")] public int SortOrder { get; set; }
}
