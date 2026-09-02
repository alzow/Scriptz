using System.Windows.Input;

namespace QueueApp.Features.Welcome.Helpers;

public partial class WelcomeCtaBlockView : ContentView
{
    public static readonly BindableProperty PrimaryTextProperty = BindableProperty.Create(
        nameof(PrimaryText), typeof(string), typeof(WelcomeCtaBlockView), string.Empty);

    public static readonly BindableProperty PrimaryCommandProperty = BindableProperty.Create(
        nameof(PrimaryCommand), typeof(ICommand), typeof(WelcomeCtaBlockView));

    public static readonly BindableProperty SecondaryTextProperty = BindableProperty.Create(
        nameof(SecondaryText), typeof(string), typeof(WelcomeCtaBlockView), string.Empty);

    public static readonly BindableProperty SecondaryCommandProperty = BindableProperty.Create(
        nameof(SecondaryCommand), typeof(ICommand), typeof(WelcomeCtaBlockView));

    public static readonly BindableProperty FootnoteTextProperty = BindableProperty.Create(
        nameof(FootnoteText), typeof(string), typeof(WelcomeCtaBlockView), string.Empty);

    public string PrimaryText
    {
        get => (string)GetValue(PrimaryTextProperty);
        set => SetValue(PrimaryTextProperty, value);
    }

    public ICommand? PrimaryCommand
    {
        get => (ICommand?)GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    public string SecondaryText
    {
        get => (string)GetValue(SecondaryTextProperty);
        set => SetValue(SecondaryTextProperty, value);
    }

    public ICommand? SecondaryCommand
    {
        get => (ICommand?)GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    public string FootnoteText
    {
        get => (string)GetValue(FootnoteTextProperty);
        set => SetValue(FootnoteTextProperty, value);
    }

    public WelcomeCtaBlockView()
    {
        InitializeComponent();
    }
}
