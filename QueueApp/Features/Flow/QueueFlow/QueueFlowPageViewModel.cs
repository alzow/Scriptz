using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow.QueueFlow;

// Operator, service, review. No day or time: a walk-in queue has no slots to pick from, and
// FlowStepEngine leaves those steps out rather than showing them skipped.
public partial class QueueFlowPageViewModel : FlowPageViewModelBase
{
    public QueueFlowPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IBookingService bookingService,
        IAuthService authService,
        IQueuePopupService popupService,
        IProfileService profileService,
        IMessenger messenger)
        : base(navigationService, secureStorageService, businessService, queueService,
            operatorService, serviceOfferingsService, bookingService, authService,
            popupService, profileService, messenger)
    {
    }

    public override async Task OnSubmittedAsync()
    {
        try
        {
            await GoToConfirmationAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
