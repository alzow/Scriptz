namespace QueueApp.Constants;

// TODO: fill these in before Account and privacy ships. Each row on that screen renders only once
// its destination is set, so nothing points at a dead link in the meantime — no URL is guessed here
// because a wrong privacy-policy address is worse than a missing row.
//
// DataRequestEmail backs "Download my data" as the interim answer to Profile §14: a Supabase
// function returning JSON is the eventual one, and this row can move to it without touching the UI.
public static class SupportLinks
{
    public const string PrivacyPolicyUrl = "";
    public const string TermsOfUseUrl = "";
    public const string TermsLastUpdated = "";
    public const string DataRequestEmail = "";

    public static bool HasPrivacyPolicy => !string.IsNullOrWhiteSpace(PrivacyPolicyUrl);
    public static bool HasTermsOfUse => !string.IsNullOrWhiteSpace(TermsOfUseUrl);
    public static bool HasDataRequestEmail => !string.IsNullOrWhiteSpace(DataRequestEmail);
}
