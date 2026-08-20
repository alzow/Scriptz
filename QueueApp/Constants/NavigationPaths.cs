namespace QueueApp.Constants;

public static class NavigationPaths
{
    // ── App start ──────────────────────────────────────────────────────────────
    public const string AppStart             = "NavigationPage/SplashScreenPage";
    public const string SplashScreenPage     = "SplashScreenPage";

    // ── Auth ───────────────────────────────────────────────────────────────────
    public const string LoginPage            = "LoginPage";
    public const string RegisterPage         = "RegisterPage";

    // ── Main (absolute — resets nav stack) ────────────────────────────────────
    public const string Dashboard            = "/NavigationPage/DashboardPage";
    public const string Login                = "/NavigationPage/LoginPage";

    // ── Feature pages (relative — pushed onto stack) ──────────────────────────
    public const string MedicationsListPage  = "MedicationsListPage";
    public const string MedicationDetailPage = "MedicationDetailPage";

    // ── Queue (operator counter-tablet) ──────────────────────────────────────
    public const string OperatorQueuePage    = "OperatorQueuePage";

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

    // ── Browse (customer-facing) ───────────────────────────────────────────────
    public const string BusinessListPage     = "BusinessListPage";
    public const string BusinessDetailPage   = "BusinessDetailPage";

    // ── Main tabbed shell ──────────────────────────────────────────────────────
    public const string MainTabbedPage       = "MainTabbedPage";
    public const string CategoryPickerPage   = "CategoryPickerPage";
    public const string HistoryPage          = "HistoryPage";
    public const string ProfilePage          = "ProfilePage";
}
