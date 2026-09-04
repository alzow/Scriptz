using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.IntakeAnswers.Helpers;

public static class IntakeAnswersHelper
{
    public const string Separator = " · ";

    // Either half can be missing — a booking with no service name, a row the board had no time
    // text for — and neither should leave a dangling separator under the customer's name.
    public static string BuildSubtitle(IntakeAnswerSnapshot snapshot) =>
        string.Join(
            Separator,
            new[] { snapshot.ServiceName, snapshot.WhenText }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
}
