namespace QueueApp.Features.Welcome.Helpers;

public partial class WelcomePanelView : ContentView
{
    public static readonly BindableProperty NumberTextProperty = BindableProperty.Create(
        nameof(NumberText), typeof(string), typeof(WelcomePanelView), string.Empty);

    public static readonly BindableProperty HeadlineTextProperty = BindableProperty.Create(
        nameof(HeadlineText), typeof(string), typeof(WelcomePanelView), string.Empty);

    public static readonly BindableProperty BodyTextProperty = BindableProperty.Create(
        nameof(BodyText), typeof(string), typeof(WelcomePanelView), string.Empty);

    public static readonly BindableProperty IllustrationSourceProperty = BindableProperty.Create(
        nameof(IllustrationSource), typeof(string), typeof(WelcomePanelView), string.Empty);

    public string NumberText
    {
        get => (string)GetValue(NumberTextProperty);
        set => SetValue(NumberTextProperty, value);
    }

    public string HeadlineText
    {
        get => (string)GetValue(HeadlineTextProperty);
        set => SetValue(HeadlineTextProperty, value);
    }

    public string BodyText
    {
        get => (string)GetValue(BodyTextProperty);
        set => SetValue(BodyTextProperty, value);
    }

    public string IllustrationSource
    {
        get => (string)GetValue(IllustrationSourceProperty);
        set => SetValue(IllustrationSourceProperty, value);
    }

    public WelcomePanelView()
    {
        InitializeComponent();
    }
}
