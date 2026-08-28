using System.Windows.Input;
using QueueApp.Shared.Domain.Models;

namespace QueueApp.Features.Flow.Helpers;

public partial class SlotPeriodView : ContentView
{
    public static readonly BindableProperty PeriodProperty = BindableProperty.Create(
        nameof(Period), typeof(SlotPeriod), typeof(SlotPeriodView));

    public static readonly BindableProperty SelectSlotCommandProperty = BindableProperty.Create(
        nameof(SelectSlotCommand), typeof(ICommand), typeof(SlotPeriodView));

    public SlotPeriod? Period
    {
        get => (SlotPeriod?)GetValue(PeriodProperty);
        set => SetValue(PeriodProperty, value);
    }

    public ICommand? SelectSlotCommand
    {
        get => (ICommand?)GetValue(SelectSlotCommandProperty);
        set => SetValue(SelectSlotCommandProperty, value);
    }

    public SlotPeriodView()
    {
        InitializeComponent();
    }
}
