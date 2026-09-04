using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow.QueueFlow;

// Operator, service, review. No day or time: a walk-in queue has no slots to pick from, and
// FlowStepEngine leaves those steps out rather than showing them skipped.
public partial class QueueFlowPageViewModel : FlowPageViewModelBase
{
    public QueueFlowPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        FlowServices services)
        : base(navigationService, secureStorageService, services)
    {
    }

    // The shop's own add has no ticket to show — the entry it just wrote is on the board behind
    // this flow, so the operator goes back to it.
    public override async Task OnSubmittedAsync()
    {
        try
        {
            if (IsOperatorFlow)
                await ReturnToTabsAsync(NavigationPaths.OperatorQueuePage);
            else
                await GoToVisitAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
