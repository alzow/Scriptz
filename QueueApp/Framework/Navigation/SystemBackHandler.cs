using QueueApp.Framework.Base;

namespace QueueApp.Framework.Navigation;

/// <summary>
/// The system back press, answered by the page's own back rather than the framework's pop.
/// </summary>
public static class SystemBackHandler
{
    // A page opened as the root of its own modal NavigationPage has nothing underneath it to pop
    // to: the back it means is a dismissal of the modal, and only its view model knows to ask for
    // one. Left unclaimed, the press goes to MPowerKit's walk of the page tree, which sends it
    // looking for the page it thinks was just navigated away from in a map it only fills in for
    // containers — that lookup throws, and a throw out of a back press takes the app down.
    //
    // The same reasoning already sits on BookingFlowPage, QueueFlowPage and VisitPage, which claim
    // the press for backs of their own; this is it for the pages whose back is simply the one their
    // header chevron performs.
    public static bool TryHandle(object? bindingContext)
    {
        try
        {
            return bindingContext is BaseViewModel viewModel && viewModel.TryHandleSystemBack();
        }
        catch (Exception)
        {
            // Unhandled leaves the press to the framework, which is a worse back than the right one
            // but a great deal better than no app.
            return false;
        }
    }
}
