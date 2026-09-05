namespace QueueApp.Features.BusinessSettings.Constants;

public static class BusinessSettingsConstants
{
    public const string ServicesTitle = "Services";
    public const string ServicesHint = "Durations decide the wait times your customers see.";
    public const string ServicesEmpty = "No services yet — add your first one below.";
    public const string AddServiceText = "Add a service";
    public const string InactiveGroupTitle = "Not offered right now";

    public const string AddServiceTitle = "Add service";

    public const string DetailsPanelTitle = "Details";
    public const string WorkflowPanelTitle = "How this service runs";
    public const string QuestionsPanelTitle = "Before they join";

    public const string NameLabel = "Service name";
    public const string NamePlaceholder = "Service name";
    public const string PriceLabel = "Price";
    public const string PricePlaceholder = "Price in Rand (optional)";
    public const string CustomDurationPlaceholder = "Minutes";

    public const string DurationLabel = "How long";
    public const string DurationHint =
        "The queue adds this up to estimate waits. Count the work, not the wait for collection.";
    public const string CustomDurationText = "Custom";

    public static readonly int[] DurationChoices = { 15, 30, 45, 60, 90 };

    public const string CollectionToggleTitle = "Customer collects afterwards";
    public const string CollectionToggleSubtitle =
        "Adds a Ready for collection step after the work is done.";
    public const string CollectionConsequence =
        "The bay frees up at Ready, so waiting customers move along. " +
        "The car sits in your yard, not in your queue.";

    public const string FlowWaiting = "Waiting";
    public const string FlowBeingDone = "Being done";
    public const string FlowReady = "Ready";
    public const string FlowCollected = "Collected";

    public const string QuestionsHint =
        "Asked while the customer is joining, so you have it before they arrive.";
    public const string QuestionsEmpty = "Nothing asked yet — this service joins straight away.";
    public const string AddQuestionText = "Add a question";

    // A service being created has no id to hang a question off, so the questions panel offers to
    // save first rather than telling the owner to go away and come back. See §4 of the spec.
    public const string SaveText = "Save";
    public const string SaveAndAddQuestionsText = "Save and add questions";
    public const string SavingText = "Saving…";

    public const string PreviewHeader = "SEE IT AS THE CUSTOMER DOES";
    public const string PreviewFormText = "Preview the form";
    public const string PreviewTitle = "Preview";
    public const string PreviewLead = "This is the form, exactly as the customer answers it.";
    public const string PreviewConditionalNote =
        "Conditional questions are shown here. The customer only sees one when the answer above it matches.";

    // Ten seconds a question is a deliberately blunt number: the point is that the owner sees the
    // cost grow as they add, not that the estimate is accurate to the second.
    public const int SecondsPerQuestion = 10;

    public const string DeactivateText = "Stop offering this";
    public const string ReactivateText = "Offer this again";
    public const string DeactivateFootnote = "Stays on past bookings and on anyone already in the queue.";

    public const string DeactivateConfirmTitle = "Stop offering this?";
    public const string DeactivateConfirmMessage =
        "It disappears from the join and booking flows. Bookings already made keep it.";
    public const string DeactivateConfirmAccept = "Stop offering it";
    public const string DeactivateConfirmCancel = "Keep it";

    public const string DurationInvalidTitle = "How long does it take?";
    public const string DurationInvalidMessage =
        "Pick one of the durations, or type a whole number of minutes.";
}
