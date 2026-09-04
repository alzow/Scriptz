using CommunityToolkit.Mvvm.Messaging;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Intake;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;

namespace QueueApp.Features.Flow.Helpers;

// One dependency for the whole flow rather than eleven repeated down every subclass constructor.
// Adding a service to the flow is a change to this file alone.
public sealed class FlowServices
{
    public IBusinessService Business { get; }
    public IQueueService Queue { get; }
    public IOperatorService Operators { get; }
    public IServiceOfferingsService ServiceOfferings { get; }
    public IBookingService Booking { get; }
    public IIntakeFieldsService IntakeFields { get; }
    public IIntakeFileService IntakeFiles { get; }
    public IAuthService Auth { get; }
    public IProfileService Profile { get; }
    public IQueuePopupService Popup { get; }
    public IMessenger Messenger { get; }

    public FlowServices(
        IBusinessService business,
        IQueueService queue,
        IOperatorService operators,
        IServiceOfferingsService serviceOfferings,
        IBookingService booking,
        IIntakeFieldsService intakeFields,
        IIntakeFileService intakeFiles,
        IAuthService auth,
        IProfileService profile,
        IQueuePopupService popup,
        IMessenger messenger)
    {
        Business = business;
        Queue = queue;
        Operators = operators;
        ServiceOfferings = serviceOfferings;
        Booking = booking;
        IntakeFields = intakeFields;
        IntakeFiles = intakeFiles;
        Auth = auth;
        Profile = profile;
        Popup = popup;
        Messenger = messenger;
    }
}
