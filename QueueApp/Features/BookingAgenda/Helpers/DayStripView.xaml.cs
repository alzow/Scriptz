using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class DayStripView : ContentView
{
    public static readonly BindableProperty DaysProperty = BindableProperty.Create(
        nameof(Days), typeof(IEnumerable), typeof(DayStripView));

    public static readonly BindableProperty SelectDayCommandProperty = BindableProperty.Create(
        nameof(SelectDayCommand), typeof(ICommand), typeof(DayStripView));

    public IEnumerable? Days
    {
        get => (IEnumerable?)GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    public ICommand? SelectDayCommand
    {
        get => (ICommand?)GetValue(SelectDayCommandProperty);
        set => SetValue(SelectDayCommandProperty, value);
    }

    public DayStripView()
    {
        InitializeComponent();
    }
}
