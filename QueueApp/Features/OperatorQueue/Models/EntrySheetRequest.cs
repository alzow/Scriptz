using QueueApp.Services.Api.Intake.Models;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.OperatorQueue.Models;

// One shape for the two things a board row can be. The waiting rows and the person in the chair
// are drawn differently and hold different fields, but every action the sheet offers works off the
// same handful, so they both collapse into this on the way in and the sheet never learns there
// were two of them.
public sealed record EntrySheetRequest
{
    public required Guid EntryId { get; init; }
    public Guid? OperatorId { get; init; }
    public Guid? ServiceId { get; init; }
    public required string CustomerName { get; init; }
    public required string Initials { get; init; }
    public required string ServiceName { get; init; }
    public required string SubText { get; init; }
    public required string WhenText { get; init; }
    public EntryStage Stage { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<IntakeAnswer> Answers { get; init; } = Array.Empty<IntakeAnswer>();

    // A resource serves one person at a time, and the board only draws the first entry it finds in
    // that state — so a second start against the same resource would take one of them off the
    // screen. A pooled entry is always startable: start_serving picks a free resource itself, and
    // says so when there is none.
    public bool CanStart { get; init; } = true;

    public bool IsInPool => OperatorId is null;
    public bool IsWaiting => Stage == EntryStage.Waiting;

    public static EntrySheetRequest FromRow(QueueRowItem row, bool canStart) => new()
    {
        EntryId = row.EntryId,
        OperatorId = row.OperatorId,
        ServiceId = row.ServiceId,
        CustomerName = row.CustomerName,
        Initials = row.Initials,
        ServiceName = row.ServiceName,
        SubText = row.SubText,
        WhenText = QueueRowItem.BuildJoinedText(row.JoinedAtText),
        Stage = EntryStage.Waiting,
        CanStart = canStart,
        Note = row.HasNote ? row.NoteText : null,
        Answers = row.IntakeAnswers,
    };

    public static EntrySheetRequest FromServingCard(ServingCardItem card, string subText, string whenText) => new()
    {
        EntryId = card.EntryId,
        OperatorId = card.OperatorId,
        ServiceId = card.ServiceId,
        CustomerName = card.CustomerName,
        Initials = TextFormat.Initials(card.CustomerName),
        ServiceName = card.ServiceText,
        SubText = subText,
        WhenText = whenText,
        Stage = EntryStage.Serving,
        Note = card.HasNote ? card.NoteText : null,
        Answers = card.IntakeAnswers,
    };
}
