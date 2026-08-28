using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Shared.Domain;

// The step list is computed per business, never fixed: a shop with one available operator asks
// nothing about operators, so the rail renders one fewer segment rather than a skipped-but-shown one.
public static class FlowStepEngine
{
    public const string QueueMode = "queue";
    public const string BookingMode = "booking";

    // is_available is a right-now flag — off on a lunch break, off between jobs. That is the right
    // filter for a customer looking at today, and the wrong one for the shop writing next Tuesday
    // into the diary, so the shop sees every active resource and lets the slot engine say what is
    // actually free.
    public static List<OperatorResponse> SelectableOperators(
        IEnumerable<OperatorResponse> operators,
        bool includeUnavailable = false) =>
        operators
            .Where(o => o.IsActive && (includeUnavailable || o.IsAvailable))
            .OrderBy(o => o.SortOrder)
            .ToList();

    public static bool ShouldAskForOperator(
        BusinessResponse business,
        IReadOnlyList<OperatorResponse> selectable,
        bool isOperatorFlow = false)
    {
        if (selectable.Count <= 1)
            return false;

        // allow_operator_choice is a customer-facing setting: it hides a choice the shop would
        // rather make for them. The shop making the booking itself has to make that choice, because
        // an operator-created booking is a direct insert and needs a real operator_id.
        return isOperatorFlow || business.AllowOperatorChoice;
    }

    public static List<FlowStep> BuildSteps(
        BusinessResponse business,
        IReadOnlyList<OperatorResponse> selectable,
        bool isOperatorFlow = false)
    {
        var steps = new List<FlowStep>();

        if (ShouldAskForOperator(business, selectable, isOperatorFlow))
            steps.Add(FlowStep.Operator);

        steps.Add(FlowStep.Service);

        if (business.Mode == BookingMode || isOperatorFlow)
        {
            steps.Add(FlowStep.Day);
            steps.Add(FlowStep.Time);
        }

        // Both modes end on Review. Booking mode used to commit straight off the Time step, which
        // left nowhere to show what was about to be requested or to say anything about it.
        steps.Add(FlowStep.Review);

        return steps;
    }

    public static string RailLabel(FlowStep step, string operatorNoun) => step switch
    {
        FlowStep.Operator => operatorNoun,
        FlowStep.Service => "Service",
        FlowStep.Day => "Day",
        FlowStep.Time => "Time",
        _ => "Confirm",
    };
}
