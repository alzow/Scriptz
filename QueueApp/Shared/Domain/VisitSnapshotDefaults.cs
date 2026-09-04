namespace QueueApp.Shared.Domain;

// What a visit shows when neither the snapshot nor the live embed can name the service or the
// person. Every "what did I do here" read resolves in the same order — snapshot column, then the
// embedded row, then one of these — so a shop that renames or deletes either one doesn't rewrite
// what the customer already did. See Documentation/historic-snapshot-backend-requirements.md.
public static class VisitSnapshotDefaults
{
    public const string ServiceNotRecorded = "Not recorded";

    // The queue assigns at join time, so an entry with nobody on it means the shop had nobody on
    // shift — not that the customer chose to take whoever was free. The booking side is the other
    // way round, hence two constants rather than one.
    public const string QueueOperatorName = "Next available";
    public const string BookingOperatorName = "Any available";
}
