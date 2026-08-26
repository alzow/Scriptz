namespace QueueApp.Features.OperatorQueue.Models;

// Tuning values the board's behaviour depends on, named rather than buried at their use site.
public static class BoardConstants
{
    // How long an unassigned entry may sit in the shared pool before the banner border goes to full
    // purple. Nothing forces anyone to take from the pool, so this readout is the only pressure
    // there is — start at ten minutes and watch it against a real shop before moving it.
    public const int PoolStarvationMinutes = 10;

    // One tick for the whole page: elapsed timers on every visible serving card, waited-for text on
    // every row, and the shop presence heartbeat, which fires every HeartbeatTicks-th tick.
    public const int TickIntervalSeconds = 1;
    public const int HeartbeatTicks = 120;

    // Below this many completed visits the "Avg" tile stays an em-dash. Mirrors the count(*) >= 3
    // guard inside operator_avg_minutes: fewer samples than this describe one haircut, not a shop.
    public const int MinimumAverageSamples = 3;

    public const string EmDash = "—";

    // PostgREST hands timestamptz back as ISO-8601 with an offset, which System.Text.Json resolves
    // to a local DateTime; an Unspecified one has come from somewhere that dropped the offset and
    // is UTC by storage. Both have to land on UTC before anything subtracts them from UtcNow —
    // otherwise every wait on the board is out by the device's offset, which in Lenasia is two
    // hours. Same normalisation the customer-facing wait readout already uses.
    public static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime();
}
