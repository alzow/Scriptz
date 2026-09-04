using QueueApp.Features.Flow.Constants;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

public sealed record FlowReview(
    string OperatorText,
    string ServiceText,
    string PriceText,
    string PositionText,
    string TurnText,
    string WhenText);

public static class FlowChoiceBuilder
{
    // The shop gets no pooled choice: get_available_slots_any returns a time, not the resource it
    // belongs to, and an operator-created booking is a direct insert that needs a real operator_id.
    // queue_entries.operator_id is nullable, so "fastest available" is a real first-class choice
    // there — join_queue reads the null id as "pick for me" and resolves it inside the insert. The
    // number shown is this app's prediction of that pick, so it is computed the same way: on-shift
    // operators only, same tie-break.
    public static List<OperatorChoiceItem> BuildOperatorChoices(
        IReadOnlyList<OperatorResponse> selectable,
        IReadOnlyList<QueueSummaryRow> summary,
        bool isQueueMode,
        bool isBookingMode,
        bool isOperatorFlow)
    {
        var choices = new List<OperatorChoiceItem>(selectable.Count + 1);

        if (!isOperatorFlow && isQueueMode)
        {
            var fastest = summary.FastestWaitMinutes();
            choices.Add(new OperatorChoiceItem
            {
                OperatorId = null,
                Name = FlowConstants.FastestAvailableName,
                Initials = FlowConstants.AnyAvailableInitials,
                SubLabel = FlowCopy.FastestAvailableSubLabel(fastest),
                IsAnyAvailable = true,
                ShowFastestTag = fastest is not null,
                IsSelected = true,
            });
        }
        else if (!isOperatorFlow && isBookingMode)
        {
            choices.Add(new OperatorChoiceItem
            {
                OperatorId = null,
                Name = FlowConstants.AnyAvailableName,
                Initials = FlowConstants.AnyAvailableInitials,
                SubLabel = FlowConstants.AnyAvailableSubLabel,
                IsAnyAvailable = true,
                ShowFastestTag = false,
                IsSelected = true,
            });
        }

        foreach (var op in selectable)
        {
            choices.Add(new OperatorChoiceItem
            {
                OperatorId = op.Id,
                Name = op.DisplayName,
                Initials = TextFormat.Initials(op.DisplayName),
                SubLabel = isBookingMode
                    ? FlowConstants.BookingOperatorSubLabel
                    : FlowCopy.QueueOperatorSubLabel(summary.FirstOrDefault(r => r.OperatorId == op.Id)),
                IsAnyAvailable = false,
                ShowFastestTag = false,
            });
        }

        return choices;
    }

    public static FlowReview BuildReview(
        ServiceChoiceItem service,
        OperatorChoiceItem? selectedOperator,
        SlotChoiceItem? selectedSlot,
        IReadOnlyList<QueueSummaryRow> summary,
        bool isSlotFlow)
    {
        var operatorText = selectedOperator?.Name ?? FlowConstants.FastestAvailableName;
        var serviceText = $"{service.Name} · {service.DurationText}";

        if (isSlotFlow)
        {
            return new FlowReview(
                operatorText,
                serviceText,
                service.PriceText,
                string.Empty,
                string.Empty,
                FlowHelper.SlotRangeText(selectedSlot));
        }

        var row = selectedOperator?.OperatorId is { } operatorId
            ? summary.FirstOrDefault(r => r.OperatorId == operatorId)
            : summary.FastestOperator();

        // No row at all means the shop has nobody on shift. The entry still joins — it waits in the
        // pool for someone to come free — but there is no line to be nth in and no honest minute
        // count to put against it, so the review says neither.
        if (row is null)
        {
            return new FlowReview(
                operatorText,
                serviceText,
                service.PriceText,
                FlowConstants.ReviewInQueueText,
                FlowConstants.ReviewNoTurnText,
                string.Empty);
        }

        var ahead = row.WaitingCount + row.ServingCount;

        return new FlowReview(
            operatorText,
            serviceText,
            service.PriceText,
            $"{TextFormat.Ordinal(ahead + 1)} in line",
            LocalTime.Now.AddMinutes(row.NewJoinWaitMinutes).ToString("HH:mm"),
            string.Empty);
    }
}
