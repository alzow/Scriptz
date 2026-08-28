using System.Windows.Input;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class TimeStepView : ContentView
{
    public static readonly BindableProperty ShowTimeStepProperty = BindableProperty.Create(
        nameof(ShowTimeStep), typeof(bool), typeof(TimeStepView), false);

    public static readonly BindableProperty IsLoadingSlotsProperty = BindableProperty.Create(
        nameof(IsLoadingSlots), typeof(bool), typeof(TimeStepView), false);

    public static readonly BindableProperty MorningProperty = BindableProperty.Create(
        nameof(Morning), typeof(SlotPeriod), typeof(TimeStepView));

    public static readonly BindableProperty AfternoonProperty = BindableProperty.Create(
        nameof(Afternoon), typeof(SlotPeriod), typeof(TimeStepView));

    public static readonly BindableProperty EveningProperty = BindableProperty.Create(
        nameof(Evening), typeof(SlotPeriod), typeof(TimeStepView));

    public static readonly BindableProperty SelectSlotCommandProperty = BindableProperty.Create(
        nameof(SelectSlotCommand), typeof(ICommand), typeof(TimeStepView));

    public bool ShowTimeStep
    {
        get => (bool)GetValue(ShowTimeStepProperty);
        set => SetValue(ShowTimeStepProperty, value);
    }

    public bool IsLoadingSlots
    {
        get => (bool)GetValue(IsLoadingSlotsProperty);
        set => SetValue(IsLoadingSlotsProperty, value);
    }

    public SlotPeriod? Morning
    {
        get => (SlotPeriod?)GetValue(MorningProperty);
        set => SetValue(MorningProperty, value);
    }

    public SlotPeriod? Afternoon
    {
        get => (SlotPeriod?)GetValue(AfternoonProperty);
        set => SetValue(AfternoonProperty, value);
    }

    public SlotPeriod? Evening
    {
        get => (SlotPeriod?)GetValue(EveningProperty);
        set => SetValue(EveningProperty, value);
    }

    public ICommand? SelectSlotCommand
    {
        get => (ICommand?)GetValue(SelectSlotCommandProperty);
        set => SetValue(SelectSlotCommandProperty, value);
    }

    public TimeStepView()
    {
        InitializeComponent();
    }
}
