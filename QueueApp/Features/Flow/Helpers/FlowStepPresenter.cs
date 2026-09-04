using QueueApp.Features.Flow.Constants;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

public readonly record struct FlowStepContext(
    FlowCopyContext Copy,
    DayChoiceItem? Day,
    SlotChoiceItem? Slot,
    int OutstandingIntakeCount,
    bool HasIntakeFields,
    string CustomerName,
    string ReviewPositionText);

public sealed record FlowChromeState(
    string RailStepLabel,
    string RailCountText,
    IReadOnlyList<RailSegment> Segments,
    IReadOnlyList<CrumbChip> Crumbs,
    string Heading,
    string Subheading);

public sealed record FlowFooterState(string Label, string Value, string CtaText, bool IsCtaEnabled);

public static class FlowStepPresenter
{
    public static FlowChromeState BuildChrome(
        IReadOnlyList<FlowStep> steps,
        int index,
        in FlowStepContext context)
    {
        var step = StepAt(steps, index);

        var segments = new List<RailSegment>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
            segments.Add(new RailSegment { IsDone = i < index, IsCurrent = i == index });

        return new FlowChromeState(
            FlowStepEngine.RailLabel(step, context.Copy.Labels.Noun),
            $"{index + 1}/{steps.Count}",
            segments,
            BuildCrumbs(steps, index, context),
            FlowCopy.StepHeading(step, context.Copy),
            FlowCopy.StepSubheading(step, context.Copy));
    }

    public static FlowFooterState BuildFooter(
        IReadOnlyList<FlowStep> steps,
        int index,
        in FlowStepContext context)
    {
        var step = StepAt(steps, index);
        var isLast = index >= steps.Count - 1;

        var cta = isLast
            ? FlowCopy.SubmitCta(context.Copy.IsOperatorFlow, context.Copy.IsBookingMode)
            : FlowConstants.NextCta;

        var service = context.Copy.Service;

        return step switch
        {
            FlowStep.Operator => new FlowFooterState(
                FlowConstants.SelectedLabel,
                context.Copy.Operator?.Name ?? FlowConstants.NothingSelectedValue,
                cta,
                context.Copy.Operator is not null),

            FlowStep.Service => new FlowFooterState(
                service is null ? FlowConstants.PickServicePrompt : $"{service.Name} · {service.DurationText}",
                service?.PriceText ?? string.Empty,
                cta,
                service is not null),

            FlowStep.Intake => new FlowFooterState(
                FlowCopy.OutstandingLabel(context.OutstandingIntakeCount),
                service?.PriceText ?? string.Empty,
                cta,
                context.OutstandingIntakeCount == 0),

            FlowStep.Day => new FlowFooterState(
                context.Day is null ? FlowConstants.PickDayPrompt : context.Day.Date.ToString("ddd d MMM"),
                context.Day?.FreeText ?? string.Empty,
                cta,
                context.Day is not null),

            FlowStep.Time => new FlowFooterState(
                FlowHelper.SlotRangeText(context.Slot),
                service?.PriceText ?? string.Empty,
                cta,
                context.Slot is not null),

            _ => BuildReviewFooter(context, cta),
        };
    }

    private static FlowFooterState BuildReviewFooter(in FlowStepContext context, string cta)
    {
        var label = context.Copy.IsOperatorFlow
            ? string.IsNullOrWhiteSpace(context.CustomerName)
                ? FlowConstants.NeedsNameLabel
                : context.CustomerName.Trim()
            : context.Copy.IsBookingMode ? FlowConstants.RequestingLabel : FlowConstants.JoiningLabel;

        var value = context.Copy.IsSlotFlow
            ? FlowHelper.SlotRangeText(context.Slot)
            : context.ReviewPositionText;

        var isEnabled = context.Copy.IsSlotFlow
            ? context.Copy.Service is not null && context.Slot is not null
            : context.Copy.Service is not null;

        return new FlowFooterState(label, value, cta, isEnabled);
    }

    private static List<CrumbChip> BuildCrumbs(
        IReadOnlyList<FlowStep> steps,
        int index,
        in FlowStepContext context)
    {
        var crumbs = new List<CrumbChip>(index);

        for (var i = 0; i < index && i < steps.Count; i++)
        {
            var text = steps[i] switch
            {
                FlowStep.Operator => context.Copy.Operator?.Name,
                FlowStep.Service => context.Copy.Service?.Name,
                FlowStep.Intake => context.HasIntakeFields ? "Details" : null,
                FlowStep.Day => context.Day is null
                    ? null
                    : $"{context.Day.DayOfWeekText} {context.Day.DayNumberText}",
                FlowStep.Time => context.Slot?.TimeText,
                _ => null,
            };

            if (!string.IsNullOrEmpty(text))
                crumbs.Add(new CrumbChip { Step = steps[i], Text = text });
        }

        return crumbs;
    }

    private static FlowStep StepAt(IReadOnlyList<FlowStep> steps, int index) => steps.Count > 0
        ? steps[Math.Clamp(index, 0, steps.Count - 1)]
        : FlowStep.Service;
}
