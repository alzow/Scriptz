namespace QueueApp.Constants;

public static class SupabaseConfig
{
    // Supabase dashboard → Settings → API
    public const string ProjectUrl = "https://lnxfwrfqxamfrbebgukg.supabase.co";
    public const string RestUrl = ProjectUrl + "/rest/v1";
    public const string AuthUrl = ProjectUrl; // GoTrue paths are /auth/v1/... off the project root
    public const string AnonKey = "sb_publishable_DtRAQzD-2sSOpD5NR6s_1A_wF7ITIbB"; // publishable/anon key — safe in-app; RLS protects data

    // SecureStorage keys for the current session, shared between AuthService and SupabaseAuthHeaderHandler
    // so the handler can read the token without depending on IAuthService (which itself depends on the
    // Refit-backed IAuthApi and would otherwise create a circular HttpClientFactory resolution).
    public const string AccessTokenKey = "sb_access_token";
    public const string RefreshTokenKey = "sb_refresh_token";
    public const string TokenExpiryKey = "sb_token_expiry_utc";
    public const string UserIdKey = "sb_user_id";
    public const string UserEmailKey = "sb_user_email";
}
