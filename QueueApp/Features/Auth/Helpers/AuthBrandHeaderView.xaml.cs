namespace QueueApp.Features.Auth.Helpers;

public partial class AuthBrandHeaderView : ContentView
{
    public static readonly BindableProperty HeadingTextProperty = BindableProperty.Create(
        nameof(HeadingText), typeof(string), typeof(AuthBrandHeaderView), string.Empty);

    public static readonly BindableProperty SubheadingTextProperty = BindableProperty.Create(
        nameof(SubheadingText), typeof(string), typeof(AuthBrandHeaderView), string.Empty);

    public string HeadingText
    {
        get => (string)GetValue(HeadingTextProperty);
        set => SetValue(HeadingTextProperty, value);
    }

    public string SubheadingText
    {
        get => (string)GetValue(SubheadingTextProperty);
        set => SetValue(SubheadingTextProperty, value);
    }

    public AuthBrandHeaderView()
    {
        InitializeComponent();
    }
}
