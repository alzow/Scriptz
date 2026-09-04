using MPowerKit;
using MPowerKit.Navigation;
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

    public override async Task OnSubmittedAsync()
    {
        try
        {
            await GoToVisitAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
