namespace QueueApp.Constants;

public static class NavigationPaths
{
    // ── App start ──────────────────────────────────────────────────────────────
    public const string AppStart             = "NavigationPage/QueueSplashPage";

    // ── Onboarding ─────────────────────────────────────────────────────────────
    public const string Welcome              = "/NavigationPage/WelcomePage";

    // ── Auth ───────────────────────────────────────────────────────────────────
    // Both relative names push onto whatever opened them, so the carousel stays underneath and the
    // back chevron works. Login is also reachable absolutely, which is how the splash, a sign-out
    // and a finished sign-up put a signed-out customer back on the root they belong on.
    public const string RegisterPage         = "RegisterPage";
    public const string LoginPage            = "LoginPage";
    public const string Login                = "/NavigationPage/LoginPage";


    // ── Queue (operator counter-tablet) ──────────────────────────────────────
    public const string OperatorQueuePage    = "OperatorQueuePage";

    // ── Booking (operator counter-tablet) ─────────────────────────────────────
    public const string BookingAgendaPage    = "BookingAgendaPage";

    // ── Business settings (Services / Staff / Hours) ──────────────────────────
    public const string BusinessSettingsPage  = "BusinessSettingsPage";
    public const string ServicesManagementPage = "ServicesManagementPage";
    public const string AddEditServicePage    = "AddEditServicePage";
    public const string AddEditIntakeFieldPage = "AddEditIntakeFieldPage";
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
    public const string VisitPage            = "VisitPage";

    // ── Profile (customer-facing) ─────────────────────────────────────────────
    public const string ProfileNotificationsPage  = "ProfileNotificationsPage";
    public const string ProfileAccountPage        = "ProfileAccountPage";

    // ── Main tabbed shell ──────────────────────────────────────────────────────
    public const string MainTabbedPage       = "MainTabbedPage";
    public const string CategoryPickerPage   = "CategoryPickerPage";
    public const string HistoryPage          = "HistoryPage";
    public const string ProfilePage          = "ProfilePage";
}
