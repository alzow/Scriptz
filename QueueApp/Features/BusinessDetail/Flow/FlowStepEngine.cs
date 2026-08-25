using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Features.BusinessDetail.Flow;

// The step list is computed per business, never fixed: a shop with one available operator asks
// nothing about operators, so the rail renders one fewer segment rather than a skipped-but-shown one.
public static class FlowStepEngine
{
    public const string QueueMode = "queue";
    public const string BookingMode = "booking";

    public static List<OperatorResponse> SelectableOperators(IEnumerable<OperatorResponse> operators) =>
        operators.Where(o => o.IsActive && o.IsAvailable).OrderBy(o => o.SortOrder).ToList();

    public static bool ShouldAskForOperator(BusinessResponse business, IReadOnlyList<OperatorResponse> selectable)
    {
        if (selectable.Count <= 1)
            return false;

        // bookings.operator_id is NOT NULL, so there is no "any available" booking the way there is
        // for a queue entry. With more than one resource free, picking correctly means "whichever
        // has the earliest slot that fits" — the multi-resource slot union that is still deferred.
        // Until that exists, ask, even where the business turned operator choice off: assigning by
        // sort_order would show the customer one bay's availability while implying it is the shop's.
        if (business.Mode == BookingMode)
            return true;

        // Spec calls this OperatorSelectionEnabled; the column that actually exists is
        // businesses.allow_operator_choice, surfaced as BusinessResponse.AllowOperatorChoice.
        return business.AllowOperatorChoice;
    }

    public static List<FlowStep> BuildSteps(BusinessResponse business, IReadOnlyList<OperatorResponse> selectable)
    {
        var steps = new List<FlowStep>();

        if (ShouldAskForOperator(business, selectable))
            steps.Add(FlowStep.Operator);

        steps.Add(FlowStep.Service);

        if (business.Mode == BookingMode)
        {
            steps.Add(FlowStep.Day);
            steps.Add(FlowStep.Time);
        }
        else
        {
            steps.Add(FlowStep.Review);
        }

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
