using System.Collections;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitTimelineView : ContentView
{
    public static readonly BindableProperty StepsProperty = BindableProperty.Create(
        nameof(Steps), typeof(IEnumerable), typeof(VisitTimelineView));

    public IEnumerable? Steps
    {
        get => (IEnumerable?)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    public VisitTimelineView()
    {
        InitializeComponent();
    }
}
