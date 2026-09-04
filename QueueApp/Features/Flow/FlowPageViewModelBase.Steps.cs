using QueueApp.Features.Flow.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase
{
    public FlowStep CurrentStep => _steps.Count > 0
        ? _steps[Math.Clamp(CurrentStepIndex, 0, _steps.Count - 1)]
        : FlowStep.Service;

    public FlowCopyContext CopyContext => new(
        IsOperatorFlow,
        IsBookingMode,
        IsSlotFlow,
        _labels,
        SelectedServiceRow,
        SelectedOperatorChoice);

    public FlowStepContext StepContext => new(
        CopyContext,
        SelectedDay,
        SelectedSlot,
        _intake.OutstandingCount,
        _intake.HasFields,
        CustomerName,
        ReviewPositionText);

    public void ApplyStep()
    {
        try
        {
            var step = CurrentStep;

            ShowOperatorStep = step == FlowStep.Operator;
            ShowServiceStep = step == FlowStep.Service;
            ShowDayStep = step == FlowStep.Day;
            ShowTimeStep = step == FlowStep.Time;
            ShowIntakeStep = step == FlowStep.Intake;
            ShowReviewStep = step == FlowStep.Review;

            // The review step's footer quotes the position this computes, so it has to run first.
            if (step == FlowStep.Review)
                RefreshReview();

            ApplyChrome();
            RefreshFooter();

            if (step == FlowStep.Day)
                _ = LoadDayCountsAsync();
            else if (step == FlowStep.Time)
                _ = LoadSlotsAsync();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void ApplyChrome()
    {
        try
        {
            var chrome = FlowStepPresenter.BuildChrome(_steps, CurrentStepIndex, StepContext);

            RailStepLabel = chrome.RailStepLabel;
            RailCountText = chrome.RailCountText;
            StepHeading = chrome.Heading;
            StepSubheading = chrome.Subheading;

            RailSegments.Clear();
            foreach (var segment in chrome.Segments)
                RailSegments.Add(segment);

            Crumbs.Clear();
            foreach (var crumb in chrome.Crumbs)
                Crumbs.Add(crumb);

            OnPropertyChanged(nameof(HasCrumbs));
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void RefreshFooter()
    {
        try
        {
            var footer = FlowStepPresenter.BuildFooter(_steps, CurrentStepIndex, StepContext);

            FooterLabel = footer.Label;
            FooterValue = footer.Value;
            FooterCtaText = footer.CtaText;
            IsFooterCtaEnabled = footer.IsCtaEnabled;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // A service that asks nothing rebuilds the list it already had, SequenceEqual says so, and not
    // a single step moves. Intake goes in after Service, so the step being stood on keeps its
    // index: the customer doesn't move, the rail gains or loses a segment ahead of them.
    public void RebuildSteps()
    {
        try
        {
            if (Business is null)
                return;

            var rebuilt = FlowStepEngine.BuildSteps(
                Business, _selectableOperators, IsOperatorFlow, HasIntakeFields);

            if (rebuilt.SequenceEqual(_steps))
                return;

            _steps = rebuilt;
            ApplyStep();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void BuildOperatorChoices()
    {
        try
        {
            var choices = FlowChoiceBuilder.BuildOperatorChoices(
                _selectableOperators, QueueSummary, IsQueueMode, IsBookingMode, IsOperatorFlow);

            OperatorChoices.Clear();
            foreach (var choice in choices)
                OperatorChoices.Add(choice);

            SelectedOperatorChoice = OperatorChoices.FirstOrDefault(c => c.IsSelected);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void RefreshReview()
    {
        try
        {
            if (SelectedServiceRow is null)
                return;

            var review = FlowChoiceBuilder.BuildReview(
                SelectedServiceRow, SelectedOperatorChoice, SelectedSlot, QueueSummary, IsSlotFlow);

            ReviewOperatorText = review.OperatorText;
            ReviewServiceText = review.ServiceText;
            ReviewPriceText = review.PriceText;
            ReviewWhenText = review.WhenText;

            if (!IsSlotFlow)
            {
                ReviewPositionText = review.PositionText;
                ReviewTurnText = review.TurnText;
            }

            OnPropertyChanged(nameof(ReviewOperatorLabel));
            OnPropertyChanged(nameof(ShowReviewWhen));
            OnPropertyChanged(nameof(ShowReviewQueueLines));
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // Every downstream clear happens here. Nothing else sets a selection back to null.
    public void InvalidateAfter(FlowStep changed)
    {
        try
        {
            switch (changed)
            {
                // Availability is per operator; services are per business, so they survive. The
                // schedule cache is keyed by both, so nothing has to be thrown away to change either.
                case FlowStep.Operator:
                case FlowStep.Service:
                    SelectedDay = null;
                    foreach (var day in DayChoices)
                        day.IsSelected = false;

                    ClearSlots();
                    break;

                case FlowStep.Day:
                    ClearSlots();
                    break;
            }
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void ClearSlots()
    {
        try
        {
            SelectedSlot = null;
            Morning = SlotPeriodFor(FlowConstants.MorningTitle, FlowConstants.MorningFromHour);
            Afternoon = SlotPeriodFor(FlowConstants.AfternoonTitle, FlowConstants.AfternoonFromHour);
            Evening = SlotPeriodFor(FlowConstants.EveningTitle, FlowConstants.EveningFromHour);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }
}
