using System.Collections;

namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitFactsSectionView : ContentView
{
    public static readonly BindableProperty SectionTitleProperty = BindableProperty.Create(
        nameof(SectionTitle), typeof(string), typeof(VisitFactsSectionView), string.Empty);

    public static readonly BindableProperty FactsProperty = BindableProperty.Create(
        nameof(Facts), typeof(IEnumerable), typeof(VisitFactsSectionView));

    public string SectionTitle
    {
        get => (string)GetValue(SectionTitleProperty);
        set => SetValue(SectionTitleProperty, value);
    }

    public IEnumerable? Facts
    {
        get => (IEnumerable?)GetValue(FactsProperty);
        set => SetValue(FactsProperty, value);
    }

    public VisitFactsSectionView()
    {
        InitializeComponent();
    }
}
