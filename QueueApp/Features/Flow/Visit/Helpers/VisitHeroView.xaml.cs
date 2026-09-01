namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitHeroView : ContentView
{
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(
        nameof(Caption), typeof(string), typeof(VisitHeroView), string.Empty);

    public static readonly BindableProperty TimeTextProperty = BindableProperty.Create(
        nameof(TimeText), typeof(string), typeof(VisitHeroView), string.Empty);

    public static readonly BindableProperty RelativeTextProperty = BindableProperty.Create(
        nameof(RelativeText), typeof(string), typeof(VisitHeroView), string.Empty);

    public static readonly BindableProperty DetailTextProperty = BindableProperty.Create(
        nameof(DetailText), typeof(string), typeof(VisitHeroView), string.Empty);

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public string TimeText
    {
        get => (string)GetValue(TimeTextProperty);
        set => SetValue(TimeTextProperty, value);
    }

    public string RelativeText
    {
        get => (string)GetValue(RelativeTextProperty);
        set => SetValue(RelativeTextProperty, value);
    }

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    public VisitHeroView()
    {
        InitializeComponent();
    }
}
