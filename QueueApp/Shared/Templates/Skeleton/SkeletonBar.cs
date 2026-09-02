namespace QueueApp.Shared.Templates.Skeleton;

public class SkeletonBar : BoxView
{
    private SkeletonPulse? _driver;

    public SkeletonBar()
    {
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        _driver?.Remove(this);
        _driver = null;

        if (Handler is null)
            return;

        _driver = SkeletonPulse.Resolve(this);
        _driver?.Add(this);
    }
}
