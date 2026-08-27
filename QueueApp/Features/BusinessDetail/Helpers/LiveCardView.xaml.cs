using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class LiveCardView : ContentView
{
    public static readonly BindableProperty IsBookingModeProperty = BindableProperty.Create(
        nameof(IsBookingMode), typeof(bool), typeof(LiveCardView), false);

    public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(
        nameof(IsOpen), typeof(bool), typeof(LiveCardView), false);

    public static readonly BindableProperty LiveCardTitleProperty = BindableProperty.Create(
        nameof(LiveCardTitle), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty LiveCardStatusProperty = BindableProperty.Create(
        nameof(LiveCardStatus), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty ShowLiveDotProperty = BindableProperty.Create(
        nameof(ShowLiveDot), typeof(bool), typeof(LiveCardView), false);

    public static readonly BindableProperty PrimaryStatValueProperty = BindableProperty.Create(
        nameof(PrimaryStatValue), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty PrimaryStatLabelProperty = BindableProperty.Create(
        nameof(PrimaryStatLabel), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty SecondaryStatValueProperty = BindableProperty.Create(
        nameof(SecondaryStatValue), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty SecondaryStatLabelProperty = BindableProperty.Create(
        nameof(SecondaryStatLabel), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty TertiaryStatValueProperty = BindableProperty.Create(
        nameof(TertiaryStatValue), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty TertiaryStatLabelProperty = BindableProperty.Create(
        nameof(TertiaryStatLabel), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty CtaTextProperty = BindableProperty.Create(
        nameof(CtaText), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty IsCtaEnabledProperty = BindableProperty.Create(
        nameof(IsCtaEnabled), typeof(bool), typeof(LiveCardView), false);

    public static readonly BindableProperty LiveFootnoteProperty = BindableProperty.Create(
        nameof(LiveFootnote), typeof(string), typeof(LiveCardView), string.Empty);

    public static readonly BindableProperty StartFlowCommandProperty = BindableProperty.Create(
        nameof(StartFlowCommand), typeof(ICommand), typeof(LiveCardView));

    public bool IsBookingMode
    {
        get => (bool)GetValue(IsBookingModeProperty);
        set => SetValue(IsBookingModeProperty, value);
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string LiveCardTitle
    {
        get => (string)GetValue(LiveCardTitleProperty);
        set => SetValue(LiveCardTitleProperty, value);
    }

    public string LiveCardStatus
    {
        get => (string)GetValue(LiveCardStatusProperty);
        set => SetValue(LiveCardStatusProperty, value);
    }

    public bool ShowLiveDot
    {
        get => (bool)GetValue(ShowLiveDotProperty);
        set => SetValue(ShowLiveDotProperty, value);
    }

    public string PrimaryStatValue
    {
        get => (string)GetValue(PrimaryStatValueProperty);
        set => SetValue(PrimaryStatValueProperty, value);
    }

    public string PrimaryStatLabel
    {
        get => (string)GetValue(PrimaryStatLabelProperty);
        set => SetValue(PrimaryStatLabelProperty, value);
    }

    public string SecondaryStatValue
    {
        get => (string)GetValue(SecondaryStatValueProperty);
        set => SetValue(SecondaryStatValueProperty, value);
    }

    public string SecondaryStatLabel
    {
        get => (string)GetValue(SecondaryStatLabelProperty);
        set => SetValue(SecondaryStatLabelProperty, value);
    }

    public string TertiaryStatValue
    {
        get => (string)GetValue(TertiaryStatValueProperty);
        set => SetValue(TertiaryStatValueProperty, value);
    }

    public string TertiaryStatLabel
    {
        get => (string)GetValue(TertiaryStatLabelProperty);
        set => SetValue(TertiaryStatLabelProperty, value);
    }

    public string CtaText
    {
        get => (string)GetValue(CtaTextProperty);
        set => SetValue(CtaTextProperty, value);
    }

    public bool IsCtaEnabled
    {
        get => (bool)GetValue(IsCtaEnabledProperty);
        set => SetValue(IsCtaEnabledProperty, value);
    }

    public string LiveFootnote
    {
        get => (string)GetValue(LiveFootnoteProperty);
        set => SetValue(LiveFootnoteProperty, value);
    }

    public ICommand? StartFlowCommand
    {
        get => (ICommand?)GetValue(StartFlowCommandProperty);
        set => SetValue(StartFlowCommandProperty, value);
    }

    public LiveCardView()
    {
        InitializeComponent();
    }
}
