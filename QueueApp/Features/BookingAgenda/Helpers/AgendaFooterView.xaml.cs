using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class AgendaFooterView : ContentView
{
    public static readonly BindableProperty IsClosedDayProperty = BindableProperty.Create(
        nameof(IsClosedDay), typeof(bool), typeof(AgendaFooterView), false);

    public static readonly BindableProperty ClosedDayTextProperty = BindableProperty.Create(
        nameof(ClosedDayText), typeof(string), typeof(AgendaFooterView), string.Empty);

    public static readonly BindableProperty IsQuietDayProperty = BindableProperty.Create(
        nameof(IsQuietDay), typeof(bool), typeof(AgendaFooterView), false);

    public static readonly BindableProperty QuietDayTextProperty = BindableProperty.Create(
        nameof(QuietDayText), typeof(string), typeof(AgendaFooterView), string.Empty);

    public static readonly BindableProperty AddBookingCommandProperty = BindableProperty.Create(
        nameof(AddBookingCommand), typeof(ICommand), typeof(AgendaFooterView));

    public static readonly BindableProperty BlockTimeCommandProperty = BindableProperty.Create(
        nameof(BlockTimeCommand), typeof(ICommand), typeof(AgendaFooterView));

    public bool IsClosedDay
    {
        get => (bool)GetValue(IsClosedDayProperty);
        set => SetValue(IsClosedDayProperty, value);
    }

    public string? ClosedDayText
    {
        get => (string?)GetValue(ClosedDayTextProperty);
        set => SetValue(ClosedDayTextProperty, value);
    }

    public bool IsQuietDay
    {
        get => (bool)GetValue(IsQuietDayProperty);
        set => SetValue(IsQuietDayProperty, value);
    }

    public string? QuietDayText
    {
        get => (string?)GetValue(QuietDayTextProperty);
        set => SetValue(QuietDayTextProperty, value);
    }

    public ICommand? AddBookingCommand
    {
        get => (ICommand?)GetValue(AddBookingCommandProperty);
        set => SetValue(AddBookingCommandProperty, value);
    }

    public ICommand? BlockTimeCommand
    {
        get => (ICommand?)GetValue(BlockTimeCommandProperty);
        set => SetValue(BlockTimeCommandProperty, value);
    }

    public AgendaFooterView()
    {
        InitializeComponent();
    }
}
