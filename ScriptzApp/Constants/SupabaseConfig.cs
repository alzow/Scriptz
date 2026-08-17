namespace ScriptzApp.Constants;

public static class SupabaseConfig
{
    // Supabase dashboard → Settings → API
    public const string ProjectUrl = "https://YOUR-PROJECT-ID.supabase.co";
    public const string RestUrl = ProjectUrl + "/rest/v1";
    public const string AuthUrl = ProjectUrl; // GoTrue paths are /auth/v1/... off the project root
    public const string AnonKey = "sb_publishable_XXXXXXXX"; // publishable/anon key — safe in-app; RLS protects data
}
