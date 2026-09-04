using CommunityToolkit.Mvvm.Input;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow;

public abstract partial class FlowPageViewModelBase
{
    [RelayCommand]
    public void SelectOperator(OperatorChoiceItem? item)
    {
        try
        {
            if (item is null || ReferenceEquals(item, SelectedOperatorChoice))
                return;

            foreach (var choice in OperatorChoices)
                choice.IsSelected = ReferenceEquals(choice, item);

            SelectedOperatorChoice = item;
            InvalidateAfter(FlowStep.Operator);
            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectService(ServiceChoiceItem? item)
    {
        try
        {
            if (item is null || ReferenceEquals(item, SelectedServiceRow))
                return;

            foreach (var row in ServiceRows)
                row.IsSelected = ReferenceEquals(row, item);

            SelectedServiceRow = item;
            InvalidateAfter(FlowStep.Service);

            _intake.BuildFor(item.Service.Id);
            OnPropertyChanged(nameof(HasIntakeFields));

            RebuildSteps();
            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectDay(DayChoiceItem? item)
    {
        try
        {
            if (item is null || !item.IsSelectable || ReferenceEquals(item, SelectedDay))
                return;

            foreach (var day in DayChoices)
                day.IsSelected = ReferenceEquals(day, item);

            SelectedDay = item;
            InvalidateAfter(FlowStep.Day);
            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectSlot(SlotChoiceItem? item)
    {
        try
        {
            if (item is null)
                return;

            foreach (var slot in Morning.Slots.Concat(Afternoon.Slots).Concat(Evening.Slots))
                slot.IsSelected = ReferenceEquals(slot, item);

            SelectedSlot = item;
            RefreshFooter();
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void SelectIntakeOption(IntakeOptionItem? option)
    {
        try
        {
            _intake.SelectOption(option);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task PickIntakeFileAsync(IntakeFieldItem? field)
    {
        try
        {
            if (SelectedServiceRow is null)
                return;

            await _intake.PickFileAsync(field, SelectedServiceRow.Service.Id);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public void ClearIntakeFile(IntakeFieldItem? field)
    {
        try
        {
            _intake.ClearFile(field);
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    // Typing into a short or long text field is the one answer nothing else routes through a
    // command, and the footer's CTA turns on the moment the last required one is filled.
    public void RefreshFooterForIntake(object? sender, EventArgs e)
    {
        if (ShowIntakeStep)
            RefreshFooter();
    }

    // PropertyChanged.Fody calls this off the woven setter, so the footer keeps up with the name
    // being typed on the review step.
    public void OnCustomerNameChanged() => RefreshFooter();
}
