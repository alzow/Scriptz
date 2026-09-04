using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Flow.Helpers;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow.BookingFlow;

// Operator, service, day, time, review. Everything but the step list and where it lands afterwards
// is the queue flow too, so it all lives on the base.
public partial class BookingFlowPageViewModel : FlowPageViewModelBase
{
    public BookingFlowPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        FlowServices services)
        : base(navigationService, secureStorageService, services)
    {
    }

    // The shop's own booking has no confirmation to show — the row it just created is already on
    // the agenda, so the operator goes back to it.
    public override async Task OnSubmittedAsync()
    {
        try
        {
            if (IsOperatorFlow)
                await ReturnToTabsAsync(NavigationPaths.BookingAgendaPage);
            else
                await GoToVisitAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
