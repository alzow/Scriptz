namespace QueueApp.Features.Flow.QueueFlow;

public partial class QueueFlowPage : ContentPage, ISystemBackButtonClickAware
{
    public QueueFlowPage()
    {
        InitializeComponent();
    }

    // Not an OnBackButtonPressed override: MPowerKit walks the page tree itself on a system back
    // press, and a leaf page that answers that walk with "handled" sends it looking for the page it
    // thinks was just navigated away from in a map it only fills in for containers. The lookup
    // throws, and a throw out of the back press takes the app down with it — which is the crash
    // stepping back through the flow used to hit. This hook is the one MPowerKit offers a page for
    // claiming the press, and claiming it here stops the walk before any of that bookkeeping runs.
    public bool OnSystemBackButtonClick()
    {
        try
        {
            return BindingContext is FlowPageViewModelBase vm && vm.TryHandleHardwareBack();
        }
        catch (Exception)
        {
            // Unhandled leaves the press to the framework, which is a worse back than the right one
            // but a great deal better than no app.
            return false;
        }
    }
}
