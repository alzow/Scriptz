using System.Collections;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitQueueStripView : ContentView
{
    public static readonly BindableProperty DotsProperty = BindableProperty.Create(
        nameof(Dots), typeof(IEnumerable), typeof(VisitQueueStripView));

    public static readonly BindableProperty PositionTextProperty = BindableProperty.Create(
        nameof(PositionText), typeof(string), typeof(VisitQueueStripView), string.Empty);

    public IEnumerable? Dots
    {
        get => (IEnumerable?)GetValue(DotsProperty);
        set => SetValue(DotsProperty, value);
    }

    public string PositionText
    {
        get => (string)GetValue(PositionTextProperty);
        set => SetValue(PositionTextProperty, value);
    }

    public VisitQueueStripView()
    {
        InitializeComponent();
    }
}
