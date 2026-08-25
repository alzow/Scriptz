using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class BookingHistoryStripView : ContentView
{
    public static readonly BindableProperty BookingsProperty = BindableProperty.Create(
        nameof(Bookings), typeof(IEnumerable), typeof(BookingHistoryStripView), default(IEnumerable));
    public static readonly BindableProperty CancelCommandProperty = BindableProperty.Create(
        nameof(CancelCommand), typeof(ICommand), typeof(BookingHistoryStripView), default(ICommand));

    public IEnumerable Bookings
    {
        get => (IEnumerable)GetValue(BookingsProperty);
        set => SetValue(BookingsProperty, value);
    }

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public BookingHistoryStripView()
    {
        InitializeComponent();
    }
}
