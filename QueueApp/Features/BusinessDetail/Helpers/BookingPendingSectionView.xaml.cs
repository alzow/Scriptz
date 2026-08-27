using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class BookingPendingSectionView : ContentView
{
    public static readonly BindableProperty HasActiveBookingProperty = BindableProperty.Create(
        nameof(HasActiveBooking), typeof(bool), typeof(BookingPendingSectionView), false);

    public static readonly BindableProperty BookingPendingBlurbProperty = BindableProperty.Create(
        nameof(BookingPendingBlurb), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingWhenTextProperty = BindableProperty.Create(
        nameof(BookingWhenText), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingEndsTextProperty = BindableProperty.Create(
        nameof(BookingEndsText), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingOperatorLabelProperty = BindableProperty.Create(
        nameof(BookingOperatorLabel), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingOperatorTextProperty = BindableProperty.Create(
        nameof(BookingOperatorText), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingServiceTextProperty = BindableProperty.Create(
        nameof(BookingServiceText), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingPriceTextProperty = BindableProperty.Create(
        nameof(BookingPriceText), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty BookingStatusLabelProperty = BindableProperty.Create(
        nameof(BookingStatusLabel), typeof(string), typeof(BookingPendingSectionView), string.Empty);

    public static readonly BindableProperty IsCancellingBookingProperty = BindableProperty.Create(
        nameof(IsCancellingBooking), typeof(bool), typeof(BookingPendingSectionView), false);

    public static readonly BindableProperty CancelBookingCommandProperty = BindableProperty.Create(
        nameof(CancelBookingCommand), typeof(ICommand), typeof(BookingPendingSectionView));

    public bool HasActiveBooking
    {
        get => (bool)GetValue(HasActiveBookingProperty);
        set => SetValue(HasActiveBookingProperty, value);
    }

    public string BookingPendingBlurb
    {
        get => (string)GetValue(BookingPendingBlurbProperty);
        set => SetValue(BookingPendingBlurbProperty, value);
    }

    public string BookingWhenText
    {
        get => (string)GetValue(BookingWhenTextProperty);
        set => SetValue(BookingWhenTextProperty, value);
    }

    public string BookingEndsText
    {
        get => (string)GetValue(BookingEndsTextProperty);
        set => SetValue(BookingEndsTextProperty, value);
    }

    public string BookingOperatorLabel
    {
        get => (string)GetValue(BookingOperatorLabelProperty);
        set => SetValue(BookingOperatorLabelProperty, value);
    }

    public string BookingOperatorText
    {
        get => (string)GetValue(BookingOperatorTextProperty);
        set => SetValue(BookingOperatorTextProperty, value);
    }

    public string BookingServiceText
    {
        get => (string)GetValue(BookingServiceTextProperty);
        set => SetValue(BookingServiceTextProperty, value);
    }

    public string BookingPriceText
    {
        get => (string)GetValue(BookingPriceTextProperty);
        set => SetValue(BookingPriceTextProperty, value);
    }

    public string BookingStatusLabel
    {
        get => (string)GetValue(BookingStatusLabelProperty);
        set => SetValue(BookingStatusLabelProperty, value);
    }

    public bool IsCancellingBooking
    {
        get => (bool)GetValue(IsCancellingBookingProperty);
        set => SetValue(IsCancellingBookingProperty, value);
    }

    public ICommand? CancelBookingCommand
    {
        get => (ICommand?)GetValue(CancelBookingCommandProperty);
        set => SetValue(CancelBookingCommandProperty, value);
    }

    public BookingPendingSectionView()
    {
        InitializeComponent();
    }
}
