namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class IdleDiscoveryBandView : ContentView
{
    public static readonly BindableProperty QuietestNowTextProperty = BindableProperty.Create(
        nameof(QuietestNowText), typeof(string), typeof(IdleDiscoveryBandView), default(string));

    public string QuietestNowText
    {
        get => (string)GetValue(QuietestNowTextProperty);
        set => SetValue(QuietestNowTextProperty, value);
    }

    public IdleDiscoveryBandView()
    {
        InitializeComponent();
    }
}
