namespace QueueApp.Shared.Templates.Skeleton;

public sealed class SkeletonPulse
{
    public const double MinOpacity = 0.42d;
    public const uint CycleMilliseconds = 1500u;

    private const string AnimationName = "QueueSkeletonPulse";

    private static readonly BindableProperty DriverProperty = BindableProperty.CreateAttached(
        "Driver", typeof(SkeletonPulse), typeof(SkeletonPulse), default(SkeletonPulse));

    private readonly List<VisualElement> _bars = new();
    private readonly Page _page;

    private bool _running;
    private double _value = 1d;

    private SkeletonPulse(Page page)
    {
        _page = page;
        _page.Appearing += OnPageAppearing;
        _page.Disappearing += OnPageDisappearing;
    }

    public static SkeletonPulse? Resolve(Element element)
    {
        var page = FindPage(element);
        if (page is null)
            return null;

        if (page.GetValue(DriverProperty) is SkeletonPulse existing)
            return existing;

        var driver = new SkeletonPulse(page);
        page.SetValue(DriverProperty, driver);
        return driver;
    }

    public static Page? FindPage(Element element)
    {
        var current = element;
        while (current is not null)
        {
            if (current is Page page)
                return page;

            current = current.Parent;
        }

        return null;
    }

    public void Add(VisualElement bar)
    {
        if (_bars.Contains(bar))
            return;

        _bars.Add(bar);
        bar.Opacity = _value;
        EnsureRunning();
    }

    public void Remove(VisualElement bar)
    {
        if (!_bars.Remove(bar))
            return;

        if (_bars.Count == 0)
            Stop();
    }

    public void EnsureRunning()
    {
        if (_running || _bars.Count == 0)
            return;

        if (ReduceMotion.IsEnabled())
        {
            Apply(1d);
            return;
        }

        _running = true;

        var pulse = new Animation();
        pulse.Add(0d, 0.5d, new Animation(Apply, 1d, MinOpacity, Easing.SinInOut));
        pulse.Add(0.5d, 1d, new Animation(Apply, MinOpacity, 1d, Easing.SinInOut));
        pulse.Commit(_page, AnimationName, length: CycleMilliseconds, repeat: () => true);
    }

    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _page.AbortAnimation(AnimationName);
        Apply(1d);
    }

    private void Apply(double value)
    {
        _value = value;

        for (var i = 0; i < _bars.Count; i++)
            _bars[i].Opacity = value;
    }

    private void OnPageAppearing(object? sender, EventArgs e) => EnsureRunning();

    private void OnPageDisappearing(object? sender, EventArgs e) => Stop();
}
