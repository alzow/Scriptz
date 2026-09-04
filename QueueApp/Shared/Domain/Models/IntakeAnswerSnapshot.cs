using QueueApp.Services.Api.Intake.Models;

namespace QueueApp.Shared.Domain.Models;

// The answers a queue entry or a booking already carries, handed from the operator board that is
// looking at them to the page that shows them. Nothing here is re-fetched: the answers are stored
// self-describing, so the row the operator tapped already holds every label, type and value the
// page renders.
public sealed record IntakeAnswerSnapshot(
    string CustomerName,
    string ServiceName,
    string WhenText,
    IReadOnlyList<IntakeAnswer> Answers)
{
    public bool HasAnswers => Answers.Count > 0;
}
