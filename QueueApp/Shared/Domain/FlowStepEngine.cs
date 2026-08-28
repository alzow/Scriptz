using QueueApp.Services.Api.Business.Models;
using QueueApp.Services.Api.Operator.Models;

namespace QueueApp.Shared.Domain;

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
