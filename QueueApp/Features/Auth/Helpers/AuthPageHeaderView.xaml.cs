namespace QueueApp.Features.Auth.Helpers;

public partial class AuthPageHeaderView : ContentView
{
    public static readonly BindableProperty TitleTextProperty = BindableProperty.Create(
        nameof(TitleText), typeof(string), typeof(AuthPageHeaderView), string.Empty);

    public static readonly BindableProperty LeadTextProperty = BindableProperty.Create(
        nameof(LeadText), typeof(string), typeof(AuthPageHeaderView), string.Empty);

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string LeadText
    {
        get => (string)GetValue(LeadTextProperty);
        set => SetValue(LeadTextProperty, value);
    }

    public AuthPageHeaderView()
    {
        InitializeComponent();
    }
}
