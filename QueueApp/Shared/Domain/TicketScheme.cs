namespace QueueApp.Shared.Domain;

// One marker in the confirmation card's "now serving → yours" strip.
public sealed record TicketMarker(string Label, bool IsNowServing, bool IsMine);

// queue_entries has no ticket-number column, so the strip cannot show the codes written on the shop
// wall (A-11 → A-14). The view renders whatever this returns, so adding a per-day sequence later is
// a second implementation of this interface rather than a change to the confirmation card.
public interface ITicketScheme
{
    IReadOnlyList<TicketMarker> BuildStrip(int myPosition);
}

// The no-schema-change default: positions as dots, the front of the queue filled, mine ringed.
public sealed class PositionTicketScheme : ITicketScheme
{
    private const int MaxMarkers = 6;

    public IReadOnlyList<TicketMarker> BuildStrip(int myPosition)
    {
        if (myPosition < 1)
            return Array.Empty<TicketMarker>();

        var first = Math.Max(1, myPosition - MaxMarkers + 1);
        var markers = new List<TicketMarker>(myPosition - first + 1);

        for (var position = first; position <= myPosition; position++)
        {
            var isMine = position == myPosition;
            markers.Add(new TicketMarker(
                Label: isMine ? "◉" : "●",
                IsNowServing: position == 1 && !isMine,
                IsMine: isMine));
        }

        return markers;
    }
}
