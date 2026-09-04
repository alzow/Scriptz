using QueueApp.Features.Flow.Constants;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

public readonly record struct FlowCopyContext(
    bool IsOperatorFlow,
    bool IsBookingMode,
    bool IsSlotFlow,
    CategoryLabelSet Labels,
    ServiceChoiceItem? Service,
    OperatorChoiceItem? Operator)
{
    public string LowerNoun => Labels.Noun.ToLowerInvariant();
}

public static class FlowCopy
{
    public static string FlowTitle(bool isOperatorFlow, bool isBookingMode) => isOperatorFlow
        ? FlowConstants.OperatorFlowTitle
        : isBookingMode ? FlowConstants.BookingFlowTitle : FlowConstants.QueueFlowTitle;

    public static string NoteLabel(bool isOperatorFlow) => isOperatorFlow
        ? FlowConstants.OperatorNoteLabel
        : FlowConstants.CustomerNoteLabel;

    public static string SubmitCta(bool isOperatorFlow, bool isBookingMode) => isOperatorFlow
        ? FlowConstants.OperatorSubmitCta
        : isBookingMode ? FlowConstants.BookingSubmitCta : FlowConstants.QueueSubmitCta;

    public static string StepHeading(FlowStep step, in FlowCopyContext context) => step switch
    {
        FlowStep.Operator => context.IsOperatorFlow
            ? $"Which {context.LowerNoun}?"
            : context.Labels.StepHeading,
        FlowStep.Service => context.IsOperatorFlow
            ? "What are they in for?"
            : "What service do you need?",
        FlowStep.Intake => context.IsOperatorFlow
            ? "What do they need to tell you?"
            : "A few details first",
        FlowStep.Day => "Which day?",
        FlowStep.Time => FlowConstants.PickTimePrompt,
        _ => context.IsOperatorFlow
            ? "Who's it for?"
            : context.IsBookingMode ? "Ready to request?" : "Ready to join?",
    };

    public static string StepSubheading(FlowStep step, in FlowCopyContext context) => step switch
    {
        FlowStep.Operator => OperatorSubheading(context),
        FlowStep.Service => context.IsSlotFlow
            ? "This helps us match the right appointment length."
            : "This helps us estimate how long you'll be in the queue.",
        FlowStep.Intake => IntakeSubheading(context),
        FlowStep.Day => $"Next {FlowConstants.DayStripLength} days. Greyed days are fully booked.",
        FlowStep.Time => context.Service is null
            ? string.Empty
            : $"{context.Service.Name} runs {context.Service.DurationText}. Times shown can fit it.",
        _ => ReviewSubheading(context),
    };

    public static string DayFineprint(in FlowCopyContext context)
    {
        if (context.Service is null || context.Operator is null)
            return string.Empty;

        var isLongJob = context.Service.Service.EstMinutes >= FlowConstants.LongJobMinutes;

        if (context.Operator.IsAnyAvailable)
        {
            return isLongJob
                ? $"A {context.Service.DurationText} job needs one unbroken block, so some days show fewer options."
                : "Counts are the shop's free slots across everyone.";
        }

        return isLongJob
            ? $"A {context.Service.DurationText} job needs one unbroken block, so some days show fewer options than {context.Operator.Name} has slots."
            : $"Counts are {context.Operator.Name}'s free slots, not the whole shop's.";
    }

    public static string OutstandingLabel(int outstanding) => outstanding switch
    {
        0 => FlowConstants.NothingOutstandingLabel,
        1 => "1 still needed",
        _ => $"{outstanding} still needed",
    };

    public static string DayFreeText(int count, string operatorName) => count == 0
        ? FlowConstants.DayFullText
        : $"{count} free · {operatorName}";

    public static string QueueOperatorSubLabel(QueueSummaryRow? summary)
    {
        if (summary is null)
            return "Free now · about 0 min";

        return (summary.WaitingCount, summary.ServingCount) switch
        {
            (0, 0) => $"Free now · about {summary.NewJoinWaitMinutes:0} min",
            (var waiting, 0) => $"{waiting} waiting · about {summary.NewJoinWaitMinutes:0} min",
            (0, var serving) => $"{serving} being served · about {summary.NewJoinWaitMinutes:0} min",
            (var waiting, var serving) =>
                $"{waiting} waiting · {serving} being served · about {summary.NewJoinWaitMinutes:0} min",
        };
    }

    public static string FastestAvailableSubLabel(double? fastestWaitMinutes) => fastestWaitMinutes is { } minutes
        ? $"Shortest wait · about {minutes:0} min"
        : FlowConstants.FastestAvailableEmptySubLabel;

    public static string EmptyPeriodNote(BusinessHours hours, DateTime? day, int fromHour)
    {
        if (day is null)
            return FlowConstants.NoSlotsNote;

        return hours.ClosingTimeOn(day.Value) is { } closing && closing.TotalHours <= fromHour
            ? $"none — shop closes {BusinessHours.FormatClock(closing)}"
            : FlowConstants.NoSlotsLongEnoughNote;
    }

    private static string OperatorSubheading(in FlowCopyContext context)
    {
        if (context.IsOperatorFlow)
            return $"Availability is per {context.LowerNoun}, so this decides which times are free.";

        return context.IsBookingMode
            ? $"Availability is per {context.LowerNoun}, so this decides which times you'll see."
            : $"Pick a {context.LowerNoun}, or take whoever's free first.";
    }

    private static string IntakeSubheading(in FlowCopyContext context)
    {
        if (context.Service is null)
            return string.Empty;

        return context.IsOperatorFlow
            ? $"{context.Service.Name} asks for these before it can be prepared."
            : $"{context.Service.Name} needs these so they can have it ready.";
    }

    private static string ReviewSubheading(in FlowCopyContext context)
    {
        if (context.IsOperatorFlow)
            return "Added by you, so it's confirmed straight away. No account means no reminder — take a number if you want to call them.";

        return context.IsBookingMode
            ? "The shop confirms this before it's final. You can cancel any time."
            : "You can leave the queue any time.";
    }
}
