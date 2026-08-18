using System.Text.Json.Serialization;

namespace ScriptzApp.Services.Api.Queue.Models;

public class EntryIdRequest
{
    [JsonPropertyName("p_entry_id")] public Guid EntryId { get; set; }
}
