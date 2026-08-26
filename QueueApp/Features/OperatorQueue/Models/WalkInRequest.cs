namespace QueueApp.Features.OperatorQueue.Models;

// Name is nullable: queue_entries.customer_name is nullable text, and a blank field stays blank
// rather than being written as the literal "Walk-in". The board falls back to "Walk-in" at display
// time instead, so a name that was given is the name that shows.
public sealed record WalkInRequest(Guid? OperatorId, string? Name, Guid ServiceId);
