namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class BookingConfirmedView : ContentView
{
    public static readonly BindableProperty OperatorNameProperty = BindableProperty.Create(
        nameof(OperatorName), typeof(string), typeof(BookingConfirmedView), default(string));
    public static readonly BindableProperty ServiceNameProperty = BindableProperty.Create(
        nameof(ServiceName), typeof(string), typeof(BookingConfirmedView), default(string));
    public static readonly BindableProperty DateDisplayProperty = BindableProperty.Create(
        nameof(DateDisplay), typeof(string), typeof(BookingConfirmedView), default(string));
    public static readonly BindableProperty TimeDisplayProperty = BindableProperty.Create(
        nameof(TimeDisplay), typeof(string), typeof(BookingConfirmedView), default(string));

    public string OperatorName
    {
        get => (string)GetValue(OperatorNameProperty);
        set => SetValue(OperatorNameProperty, value);
    }

    public string ServiceName
    {
        get => (string)GetValue(ServiceNameProperty);
        set => SetValue(ServiceNameProperty, value);
    }

    public string DateDisplay
    {
        get => (string)GetValue(DateDisplayProperty);
        set => SetValue(DateDisplayProperty, value);
    }

    public string TimeDisplay
    {
        get => (string)GetValue(TimeDisplayProperty);
        set => SetValue(TimeDisplayProperty, value);
    }

    public BookingConfirmedView()
    {
        InitializeComponent();
    }
}
