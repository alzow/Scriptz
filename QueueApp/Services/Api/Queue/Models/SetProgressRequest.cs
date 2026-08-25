using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

public class SetProgressRequest
{
    [JsonPropertyName("p_entry_id")] public Guid EntryId { get; set; }
    [JsonPropertyName("p_status")] public string? Status { get; set; }
}
