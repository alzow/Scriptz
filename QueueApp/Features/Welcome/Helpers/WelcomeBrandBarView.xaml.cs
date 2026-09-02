namespace QueueApp.Features.Welcome.Helpers;

public partial class WelcomeBrandBarView : ContentView
{
    public static readonly BindableProperty BrandTextProperty = BindableProperty.Create(
        nameof(BrandText), typeof(string), typeof(WelcomeBrandBarView), string.Empty);

    public static readonly BindableProperty EyebrowTextProperty = BindableProperty.Create(
        nameof(EyebrowText), typeof(string), typeof(WelcomeBrandBarView), string.Empty);

    public string BrandText
    {
        get => (string)GetValue(BrandTextProperty);
        set => SetValue(BrandTextProperty, value);
    }

    public string EyebrowText
    {
        get => (string)GetValue(EyebrowTextProperty);
        set => SetValue(EyebrowTextProperty, value);
    }

    public WelcomeBrandBarView()
    {
        InitializeComponent();
    }
}
