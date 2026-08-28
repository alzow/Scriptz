using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Flow;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BookingFlow;

// Operator, service, day, time, review. Everything but the step list and where it lands
// afterwards is the queue flow too, so it all lives on the base.
public partial class BookingFlowPageViewModel : FlowPageViewModelBase
{
    public BookingFlowPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IQueueService queueService,
        IOperatorService operatorService,
        IServiceOfferingsService serviceOfferingsService,
        IBookingService bookingService,
        IAuthService authService,
        IQueuePopupService popupService,
        IProfileService profileService)
        : base(navigationService, secureStorageService, businessService, queueService,
            operatorService, serviceOfferingsService, bookingService, authService,
            popupService, profileService)
    {
    }

    // Submitting replaces this page rather than stacking on it: backing out of the confirmation
    // should land on the business, not on a flow that has already been committed.
    public override async Task OnSubmittedAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
            await NavigationService.NavigateAsync(NavigationPaths.ConfirmationPage, new NavigationParameters
            {
                { NavigationKeys.BusinessId, BusinessId },
            });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
