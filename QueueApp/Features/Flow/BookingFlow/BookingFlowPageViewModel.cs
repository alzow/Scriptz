using CommunityToolkit.Mvvm.Messaging;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Flow.BookingFlow;

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
        IProfileService profileService,
        IIntakeFieldsService intakeFieldsService,
        IIntakeFileService intakeFileService,
        IMessenger messenger)
        : base(navigationService, secureStorageService, businessService, queueService,
            operatorService, serviceOfferingsService, bookingService, authService,
            popupService, profileService, intakeFieldsService, intakeFileService, messenger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
