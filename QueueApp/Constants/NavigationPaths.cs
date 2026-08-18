namespace ScriptzApp.Constants;

public static class NavigationPaths
{
    // ── App start ──────────────────────────────────────────────────────────────
    public const string AppStart             = "NavigationPage/LoginPage";

    // ── Auth ───────────────────────────────────────────────────────────────────
    public const string LoginPage            = "LoginPage";
    public const string RegisterPage         = "RegisterPage";

    // ── Main (absolute — resets nav stack) ────────────────────────────────────
    public const string Dashboard            = "/NavigationPage/DashboardPage";
    public const string Login                = "/NavigationPage/LoginPage";

    // ── Feature pages (relative — pushed onto stack) ──────────────────────────
    public const string MedicationsListPage  = "MedicationsListPage";
    public const string MedicationDetailPage = "MedicationDetailPage";

    // ── Queue (barber counter-tablet) ─────────────────────────────────────────
    public const string BarberQueuePage      = "BarberQueuePage";
}
