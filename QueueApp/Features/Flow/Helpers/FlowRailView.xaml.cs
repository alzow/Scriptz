using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.Flow.Helpers;

public partial class FlowRailView : ContentView
{
    public static readonly BindableProperty RailSegmentsProperty = BindableProperty.Create(
        nameof(RailSegments), typeof(IEnumerable), typeof(FlowRailView));

    public static readonly BindableProperty RailStepLabelProperty = BindableProperty.Create(
        nameof(RailStepLabel), typeof(string), typeof(FlowRailView), string.Empty);

    public static readonly BindableProperty RailCountTextProperty = BindableProperty.Create(
        nameof(RailCountText), typeof(string), typeof(FlowRailView), string.Empty);

    public static readonly BindableProperty CrumbsProperty = BindableProperty.Create(
        nameof(Crumbs), typeof(IEnumerable), typeof(FlowRailView));

    public static readonly BindableProperty HasCrumbsProperty = BindableProperty.Create(
        nameof(HasCrumbs), typeof(bool), typeof(FlowRailView), false);

    public static readonly BindableProperty StepHeadingProperty = BindableProperty.Create(
        nameof(StepHeading), typeof(string), typeof(FlowRailView), string.Empty);

    public static readonly BindableProperty StepSubheadingProperty = BindableProperty.Create(
        nameof(StepSubheading), typeof(string), typeof(FlowRailView), string.Empty);

    public static readonly BindableProperty JumpToCrumbCommandProperty = BindableProperty.Create(
        nameof(JumpToCrumbCommand), typeof(ICommand), typeof(FlowRailView));

    public IEnumerable? RailSegments
    {
        get => (IEnumerable?)GetValue(RailSegmentsProperty);
        set => SetValue(RailSegmentsProperty, value);
    }

    public string RailStepLabel
    {
        get => (string)GetValue(RailStepLabelProperty);
        set => SetValue(RailStepLabelProperty, value);
    }

    public string RailCountText
    {
        get => (string)GetValue(RailCountTextProperty);
        set => SetValue(RailCountTextProperty, value);
    }

    public IEnumerable? Crumbs
    {
        get => (IEnumerable?)GetValue(CrumbsProperty);
        set => SetValue(CrumbsProperty, value);
    }

    public bool HasCrumbs
    {
        get => (bool)GetValue(HasCrumbsProperty);
        set => SetValue(HasCrumbsProperty, value);
    }

    public string StepHeading
    {
        get => (string)GetValue(StepHeadingProperty);
        set => SetValue(StepHeadingProperty, value);
    }

    public string StepSubheading
    {
        get => (string)GetValue(StepSubheadingProperty);
        set => SetValue(StepSubheadingProperty, value);
    }

    public ICommand? JumpToCrumbCommand
    {
        get => (ICommand?)GetValue(JumpToCrumbCommandProperty);
        set => SetValue(JumpToCrumbCommandProperty, value);
    }

    public FlowRailView()
    {
        InitializeComponent();
    }
}
