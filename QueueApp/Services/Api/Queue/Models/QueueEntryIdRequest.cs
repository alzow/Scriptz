using System.Text.Json.Serialization;

namespace QueueApp.Services.Api.Queue.Models;

// queue_entry_wait_minutes takes "entry_id", unlike EntryIdRequest's "p_entry_id".
public class QueueEntryIdRequest
{
    [JsonPropertyName("entry_id")] public Guid EntryId { get; set; }
}
