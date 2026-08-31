using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.Settings.Models;

public class StaffRow
{
    public Guid Id { get; }
    public string DisplayName { get; }
    public string Initials { get; }
    public string ShiftText { get; }
    public int SortOrder { get; set; }
    public bool IsReactivating { get; set; }

    public StaffRow(OperatorResponse source)
    {
        Id = source.Id;
        DisplayName = source.DisplayName;
        Initials = InitialsOf(source.DisplayName);
        ShiftText = source.IsAvailable ? "On shift" : "Off shift";
        SortOrder = source.SortOrder;
    }

    private static string InitialsOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
