using QueueApp.Shared.Templates.Skeleton.Archetypes;

namespace QueueApp.Shared.Templates.Skeleton;

public class SkeletonList : ContentView
{
    public const int DefaultCount = 6;

    private const string TileStripStyleKey = "hsl_TileStrip";
    private const string DayStripStyleKey = "hsl_DayStrip";
    private const string SectionRowStackStyleKey = "vsl_SectionRowStack";

    public static readonly BindableProperty KindProperty = BindableProperty.Create(
        nameof(Kind), typeof(SkeletonKind), typeof(SkeletonList), SkeletonKind.ListRow,
        propertyChanged: (bindable, _, _) => ((SkeletonList)bindable).Rebuild());

    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count), typeof(int), typeof(SkeletonList), DefaultCount,
        propertyChanged: (bindable, _, _) => ((SkeletonList)bindable).Rebuild());

    public SkeletonKind Kind
    {
        get => (SkeletonKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public SkeletonList()
    {
        SemanticProperties.SetDescription(this, "Loading");
        Rebuild();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsVisibleProperty.PropertyName)
            Rebuild();
    }

    public void Rebuild()
    {
        if (!IsVisible)
        {
            Content = null;
            return;
        }

        var kind = Kind;
        var count = Math.Max(1, Count);

        Layout container = kind switch
        {
            SkeletonKind.Tile => new HorizontalStackLayout { Style = FindStyle(TileStripStyleKey) },
            SkeletonKind.DayTile => new HorizontalStackLayout { Style = FindStyle(DayStripStyleKey) },
            SkeletonKind.SectionRow => new VerticalStackLayout { Style = FindStyle(SectionRowStackStyleKey) },
            _ => new VerticalStackLayout(),
        };

        for (var i = 0; i < count; i++)
            container.Add(Create(kind));

        Content = container;
    }

    public static View Create(SkeletonKind kind) => kind switch
    {
        SkeletonKind.ListRowPlain => new SkeletonListRowPlainView(),
        SkeletonKind.TimeRow => new SkeletonTimeRowView(),
        SkeletonKind.BoardRow => new SkeletonBoardRowView(),
        SkeletonKind.Card => new SkeletonCardView(),
        SkeletonKind.FactRow => new SkeletonFactRowView(),
        SkeletonKind.Hero => new SkeletonHeroView(),
        SkeletonKind.SectionRow => new SkeletonSectionRowView(),
        SkeletonKind.Tile => new SkeletonTileView(),
        SkeletonKind.DayTile => new SkeletonDayTileView(),
        SkeletonKind.StatsBar => new SkeletonStatsBarView(),
        _ => new SkeletonListRowView(),
    };

    private static Style? FindStyle(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var style) == true
            ? style as Style
            : null;
}
