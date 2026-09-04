namespace QueueApp.Shared.Domain;

public static class TextFormat
{
    public const string UnknownInitials = "?";

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return UnknownInitials;

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return UnknownInitials;

        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    public static string Ordinal(int value) => value switch
    {
        11 or 12 or 13 => $"{value}th",
        _ when value % 10 == 1 => $"{value}st",
        _ when value % 10 == 2 => $"{value}nd",
        _ when value % 10 == 3 => $"{value}rd",
        _ => $"{value}th",
    };

    public static string Plural(int count, string noun) =>
        $"{count} {noun}{(count == 1 ? string.Empty : "s")}";

    public static string Join(string first, string second) =>
        second.Length == 0 ? first : $"{first} · {second}";

    public static string? FirstNonBlank(string? first, string? second) =>
        !string.IsNullOrWhiteSpace(first) ? first
        : string.IsNullOrWhiteSpace(second) ? null
        : second;
}
