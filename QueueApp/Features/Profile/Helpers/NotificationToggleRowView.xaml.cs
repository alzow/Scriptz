namespace QueueApp.Features.Profile.Helpers;

public partial class NotificationToggleRowView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(NotificationToggleRowView), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty DetailProperty = BindableProperty.Create(
        nameof(Detail), typeof(string), typeof(NotificationToggleRowView), string.Empty);

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public static readonly BindableProperty IsOnProperty = BindableProperty.Create(
        nameof(IsOn), typeof(bool), typeof(NotificationToggleRowView), true,
        defaultBindingMode: BindingMode.TwoWay);

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public static readonly BindableProperty IsAvailableProperty = BindableProperty.Create(
        nameof(IsAvailable), typeof(bool), typeof(NotificationToggleRowView), true);

    public bool IsAvailable
    {
        get => (bool)GetValue(IsAvailableProperty);
        set => SetValue(IsAvailableProperty, value);
    }

    public NotificationToggleRowView()
    {
        InitializeComponent();
    }
}
