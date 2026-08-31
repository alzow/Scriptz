namespace QueueApp.Constants;

public static class NavigationPaths
{
    // ── App start ──────────────────────────────────────────────────────────────
    public const string AppStart             = "NavigationPage/QueueSplashPage";

    // ── Auth ───────────────────────────────────────────────────────────────────
    public const string RegisterPage         = "RegisterPage";
    public const string Login                = "/NavigationPage/LoginPage";


    // ── Queue (operator counter-tablet) ──────────────────────────────────────
    public const string OperatorQueuePage    = "OperatorQueuePage";

    // ── Booking (operator counter-tablet) ─────────────────────────────────────
    public const string BookingAgendaPage    = "BookingAgendaPage";

    // ── Business settings (Services / Staff / Hours) ──────────────────────────
    public const string BusinessSettingsPage  = "BusinessSettingsPage";
    public const string ServicesManagementPage = "ServicesManagementPage";
    public const string AddEditServicePage    = "AddEditServicePage";
    public const string StaffManagementPage   = "StaffManagementPage";
    public const string AddEditOperatorPage   = "AddEditOperatorPage";
    public const string OperatorHoursPage     = "OperatorHoursPage";
    public const string WeeklyHoursPage       = "WeeklyHoursPage";
    public const string AddAvailabilityWindowPage = "AddAvailabilityWindowPage";
    public const string BlockedDatesPage       = "BlockedDatesPage";
    public const string AddAvailabilityBlockPage = "AddAvailabilityBlockPage";
    public const string BusinessLocationPage   = "BusinessLocationPage";

    // ── Browse (customer-facing) ───────────────────────────────────────────────
    public const string BusinessDetailPage   = "BusinessDetailPage";
    public const string BookingFlowPage      = "BookingFlowPage";
    public const string QueueFlowPage        = "QueueFlowPage";
    public const string ConfirmationPage     = "ConfirmationPage";

    // ── Main tabbed shell ──────────────────────────────────────────────────────
    public const string MainTabbedPage       = "MainTabbedPage";
    public const string CategoryPickerPage   = "CategoryPickerPage";
    public const string HistoryPage          = "HistoryPage";
    public const string ProfilePage          = "ProfilePage";
}
