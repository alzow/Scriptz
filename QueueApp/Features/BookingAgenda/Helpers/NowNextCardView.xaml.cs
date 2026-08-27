using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class NowNextCardView : ContentView
{
    public static readonly BindableProperty HasCardProperty = BindableProperty.Create(
        nameof(HasCard), typeof(bool), typeof(NowNextCardView), false);

    public static readonly BindableProperty CardKickerProperty = BindableProperty.Create(
        nameof(CardKicker), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardNameProperty = BindableProperty.Create(
        nameof(CardName), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardSubtitleProperty = BindableProperty.Create(
        nameof(CardSubtitle), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardMetaProperty = BindableProperty.Create(
        nameof(CardMeta), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardTimerTextProperty = BindableProperty.Create(
        nameof(CardTimerText), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardTimerCaptionProperty = BindableProperty.Create(
        nameof(CardTimerCaption), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty CardActionTextProperty = BindableProperty.Create(
        nameof(CardActionText), typeof(string), typeof(NowNextCardView), string.Empty);

    public static readonly BindableProperty IsCardEnabledProperty = BindableProperty.Create(
        nameof(IsCardEnabled), typeof(bool), typeof(NowNextCardView), true);

    public static readonly BindableProperty CardActionCommandProperty = BindableProperty.Create(
        nameof(CardActionCommand), typeof(ICommand), typeof(NowNextCardView));

    public bool HasCard
    {
        get => (bool)GetValue(HasCardProperty);
        set => SetValue(HasCardProperty, value);
    }

    public string? CardKicker
    {
        get => (string?)GetValue(CardKickerProperty);
        set => SetValue(CardKickerProperty, value);
    }

    public string? CardName
    {
        get => (string?)GetValue(CardNameProperty);
        set => SetValue(CardNameProperty, value);
    }

    public string? CardSubtitle
    {
        get => (string?)GetValue(CardSubtitleProperty);
        set => SetValue(CardSubtitleProperty, value);
    }

    public string? CardMeta
    {
        get => (string?)GetValue(CardMetaProperty);
        set => SetValue(CardMetaProperty, value);
    }

    public string? CardTimerText
    {
        get => (string?)GetValue(CardTimerTextProperty);
        set => SetValue(CardTimerTextProperty, value);
    }

    public string? CardTimerCaption
    {
        get => (string?)GetValue(CardTimerCaptionProperty);
        set => SetValue(CardTimerCaptionProperty, value);
    }

    public string? CardActionText
    {
        get => (string?)GetValue(CardActionTextProperty);
        set => SetValue(CardActionTextProperty, value);
    }

    public bool IsCardEnabled
    {
        get => (bool)GetValue(IsCardEnabledProperty);
        set => SetValue(IsCardEnabledProperty, value);
    }

    public ICommand? CardActionCommand
    {
        get => (ICommand?)GetValue(CardActionCommandProperty);
        set => SetValue(CardActionCommandProperty, value);
    }

    public NowNextCardView()
    {
        InitializeComponent();
    }
}
