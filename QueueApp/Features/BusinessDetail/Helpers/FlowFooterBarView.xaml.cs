using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class FlowFooterBarView : ContentView
{
    public static readonly BindableProperty IsFlowActiveProperty = BindableProperty.Create(
        nameof(IsFlowActive), typeof(bool), typeof(FlowFooterBarView), false);

    public static readonly BindableProperty FooterLabelProperty = BindableProperty.Create(
        nameof(FooterLabel), typeof(string), typeof(FlowFooterBarView), string.Empty);

    public static readonly BindableProperty FooterValueProperty = BindableProperty.Create(
        nameof(FooterValue), typeof(string), typeof(FlowFooterBarView), string.Empty);

    public static readonly BindableProperty FooterCtaTextProperty = BindableProperty.Create(
        nameof(FooterCtaText), typeof(string), typeof(FlowFooterBarView), string.Empty);

    public static readonly BindableProperty IsSubmittingProperty = BindableProperty.Create(
        nameof(IsSubmitting), typeof(bool), typeof(FlowFooterBarView), false);

    public static readonly BindableProperty IsFooterCtaEnabledProperty = BindableProperty.Create(
        nameof(IsFooterCtaEnabled), typeof(bool), typeof(FlowFooterBarView), false);

    public static readonly BindableProperty NextCommandProperty = BindableProperty.Create(
        nameof(NextCommand), typeof(ICommand), typeof(FlowFooterBarView));

    public bool IsFlowActive
    {
        get => (bool)GetValue(IsFlowActiveProperty);
        set => SetValue(IsFlowActiveProperty, value);
    }

    public string FooterLabel
    {
        get => (string)GetValue(FooterLabelProperty);
        set => SetValue(FooterLabelProperty, value);
    }

    public string FooterValue
    {
        get => (string)GetValue(FooterValueProperty);
        set => SetValue(FooterValueProperty, value);
    }

    public string FooterCtaText
    {
        get => (string)GetValue(FooterCtaTextProperty);
        set => SetValue(FooterCtaTextProperty, value);
    }

    public bool IsSubmitting
    {
        get => (bool)GetValue(IsSubmittingProperty);
        set => SetValue(IsSubmittingProperty, value);
    }

    public bool IsFooterCtaEnabled
    {
        get => (bool)GetValue(IsFooterCtaEnabledProperty);
        set => SetValue(IsFooterCtaEnabledProperty, value);
    }

    public ICommand? NextCommand
    {
        get => (ICommand?)GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public FlowFooterBarView()
    {
        InitializeComponent();
    }
}
