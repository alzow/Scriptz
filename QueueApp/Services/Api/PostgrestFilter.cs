namespace QueueApp.Services.Api;

// PostgREST takes its filters as `<operator>.<value>` in the query string. Building those by hand
// at three dozen call sites is how "eq." ends up spelt "eq:" once, so every filter this app sends
// is built here.
public static class PostgrestFilter
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string OffsetTimestampFormat = "yyyy-MM-ddTHH:mm:sszzz";
    public const string UtcTimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static string Eq(Guid value) => $"eq.{value}";

    public static string Eq(string value) => $"eq.{value}";

    public static string Eq(bool value) => $"eq.{(value ? "true" : "false")}";

    public static string In(IEnumerable<Guid> values) => $"in.({string.Join(',', values)})";

    public static string Gte(DateTimeOffset value) => $"gte.{value.ToString(OffsetTimestampFormat)}";

    public static string GteUtc(DateTime value) => $"gte.{value.ToString(UtcTimestampFormat)}";

    public static string Date(DateTime value) => value.ToString(DateFormat);

    // PostgREST needs both halves of a range in one `and=(…)` group — two separate query parameters
    // on the same column collide rather than intersect.
    public static string StartsWithin(DateTimeOffset from, DateTimeOffset until) =>
        And($"starts_at.gte.{from.ToString(OffsetTimestampFormat)}",
            $"starts_at.lt.{until.ToString(OffsetTimestampFormat)}");

    // Overlap, not containment: something that started yesterday and runs through this morning
    // still covers this morning.
    public static string OverlapsRange(DateTimeOffset from, DateTimeOffset until) =>
        And($"starts_at.lt.{until.ToString(OffsetTimestampFormat)}",
            $"ends_at.gt.{from.ToString(OffsetTimestampFormat)}");

    public static string And(params string[] clauses) => $"({string.Join(',', clauses)})";
}
