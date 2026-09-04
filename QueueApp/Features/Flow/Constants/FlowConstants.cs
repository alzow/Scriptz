namespace QueueApp.Features.Flow.Constants;

public static class FlowConstants
{
    public const int DayStripLength = 14;
    public const int SlotDebounceMilliseconds = 250;
    public const int LongJobMinutes = 120;

    public const int MorningFromHour = 0;
    public const int MorningToHour = 12;
    public const int AfternoonFromHour = 12;
    public const int AfternoonToHour = 17;
    public const int EveningFromHour = 17;
    public const int EveningToHour = 24;

    public const string MorningTitle = "MORNING";
    public const string AfternoonTitle = "AFTERNOON";
    public const string EveningTitle = "EVENING";

    public const string CreatedByOperator = "operator";

    public const string AnyAvailableInitials = "★";
    public const string UnknownInitials = "?";

    public const string FastestAvailableName = "Fastest available";
    public const string FastestAvailableEmptySubLabel = "Nobody on shift right now";
    public const string AnyAvailableName = "Any available";
    public const string AnyAvailableSubLabel = "Whoever's free at that time";
    public const string BookingOperatorSubLabel = "Tap to see their times";

    public const string OperatorFlowTitle = "Add a booking";
    public const string BookingFlowTitle = "Book a slot";
    public const string QueueFlowTitle = "Join the queue";

    public const string NextCta = "Next";
    public const string OperatorSubmitCta = "Add booking";
    public const string BookingSubmitCta = "Request booking";
    public const string QueueSubmitCta = "Join queue";

    public const string PickTimePrompt = "Pick a time";
    public const string PickDayPrompt = "Pick a day";
    public const string PickServicePrompt = "Pick a service";
    public const string SelectedLabel = "Selected";
    public const string NothingSelectedValue = "Nothing yet";
    public const string NothingOutstandingLabel = "Nothing else needed";
    public const string NeedsNameLabel = "Needs a name";
    public const string RequestingLabel = "Requesting";
    public const string JoiningLabel = "Joining as";

    public const string NoSlotsNote = "none";
    public const string NoSlotsLongEnoughNote = "none — nothing long enough left";
    public const string DayFullText = "full";

    public const string OperatorNoteLabel = "ADDITIONAL DETAILS — OPTIONAL";
    public const string CustomerNoteLabel = "ANYTHING THEY SHOULD KNOW — OPTIONAL";

    public const string ReviewInQueueText = "in the queue";
    public const string ReviewNoTurnText = "—";

    public const string ErrorAlertTitle = "Couldn't do that";
    public const string MissingBusinessIdError = "A flow page requires a 'businessId' parameter.";
    public const string BusinessGoneError = "That business is no longer available.";
    public const string NoSignedInUserError = "No signed-in user id — should never happen post-splash-gate.";
    public const string NoCustomerNameError =
        "A name is needed — it's all the agenda has to show for a booking with no account behind it.";
    public const string NoOperatorForBookingError = "Pick who's doing the work before booking.";
    public const string SlotTakenByShopError = "That slot was just taken — please pick another time.";
    public const string SlotTakenByCustomerError =
        "That slot was just booked by someone else — please pick another time.";
}
