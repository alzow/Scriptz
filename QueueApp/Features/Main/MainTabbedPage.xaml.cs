namespace QueueApp.Features.Main;

public partial class MainTabbedPage : TabbedPage
{
    public MainTabbedPage()
    {
        InitializeComponent();
    }

#if IOS
    private bool _hasRemeasuredTabs;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // iOS 18+'s floating tab bar measures each UITabBarItem's title label
        // against stale sizing the first time tabs are populated dynamically (as
        // ours are, via MPowerKit navigation), so labels render clipped until the
        // user taps that specific tab and UIKit remeasures it. Each item is only
        // remeasured once it becomes selected, so we replay that selection for
        // every tab. Switches happen back-to-back with no delay between them so
        // no intermediate tab is ever actually rendered/painted, only the final one.
        //
        // Once per page, not once per Appearing: this page now stays alive under the
        // modals pushed over it, so Appearing fires again every time one is dismissed
        // and replaying the sweep there would churn every tab's feed for nothing.
        if (_hasRemeasuredTabs)
            return;

        var current = CurrentPage;
        if (current is null || Children.Count < 2)
            return;

        _hasRemeasuredTabs = true;

        await Task.Delay(50);

        foreach (var page in Children)
            CurrentPage = page;

        CurrentPage = current;
    }
#endif
}
