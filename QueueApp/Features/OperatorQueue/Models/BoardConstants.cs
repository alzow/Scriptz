namespace QueueApp.Features.OperatorQueue.Models;

public static class BoardConstants
{
    public const int PoolStarvationMinutes = 10;

    public const int TickIntervalSeconds = 1;
    public const int HeartbeatTicks = 120;

    public const int MinimumAverageSamples = 3;

    public const string EmDash = "—";

    public const string NowServingText = "in the chair now";

    public static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime();
}
