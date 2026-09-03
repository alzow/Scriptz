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

    // hasIntakeFields is the whole of this feature's reach into the step list: a service that
    // defines questions gets one more step between picking it and confirming, and a service that
    // defines none — which is every service that exists today — builds the list it built before.
    public static List<FlowStep> BuildSteps(
        BusinessResponse business,
        IReadOnlyList<OperatorResponse> selectable,
        bool isOperatorFlow = false,
        bool hasIntakeFields = false)
    {
        var steps = new List<FlowStep>();

        if (ShouldAskForOperator(business, selectable, isOperatorFlow))
            steps.Add(FlowStep.Operator);

        steps.Add(FlowStep.Service);

        // Straight after the service in both modes, not before the confirm: the questions belong to
        // the service that raised them, so stepping back off them lands on the choice that did.
        if (hasIntakeFields)
            steps.Add(FlowStep.Intake);

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
        FlowStep.Intake => "Details",
        FlowStep.Day => "Day",
        FlowStep.Time => "Time",
        _ => "Confirm",
    };
}
