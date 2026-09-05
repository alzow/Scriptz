namespace QueueApp.Framework.Navigation;

/// <summary>
/// One page transition at a time, app-wide.
/// </summary>
public static class NavigationGate
{
    // MPowerKit keeps its own record of what sits on each navigation stack alongside MAUI's. A
    // second transition that starts while the first is still settling pops a page the library has
    // already written off, the two records disagree from that moment on, and nothing fails yet —
    // the crash lands later, on whichever navigation next reads the record, which is why it shows
    // up as "the app died leaving business detail" rather than "the app died in the flow".
    //
    // Every back in the app is one tap away from being issued twice: a chevron double-tap, the
    // chevron plus Android's hardware back, a step-back command that fires the pop without awaiting
    // it. The gate is what makes the second one harmless.
    //
    // Dropped rather than queued: the second tap of a double-tap means "go back", and the first tap
    // is already going back. Running it afterwards would go back twice, which is the bug.
    private static int _inFlight;
    private static DateTime _lastCompletedUtc = DateTime.MinValue;

    // A pop's task completes when MAUI hands the page back, a beat before the platform has finished
    // the transition and the destination's own back button is live again. Taps that land in that
    // beat are the ones that used to get through.
    private static readonly TimeSpan SettleWindow = TimeSpan.FromMilliseconds(350);

    // Flows down the async call chain, so a gated transition that calls another one — a return to
    // the tabs deciding between a dismissal and a rebuilt shell — is treated as the one transition
    // it is instead of the inner call being dropped by the gate its own caller is holding.
    private static readonly AsyncLocal<bool> Inside = new();

    /// <summary>
    /// Runs <paramref name="navigation"/> if no other transition holds the gate. Returns false when
    /// the transition was dropped, which callers are free to ignore — a dropped back is a back the
    /// app is already performing.
    /// </summary>
    public static async Task<bool> RunAsync(Func<Task> navigation)
    {
        if (Inside.Value)
        {
            await navigation();
            return true;
        }

        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) == 1)
            return false;

        try
        {
            if (DateTime.UtcNow - _lastCompletedUtc < SettleWindow)
                return false;

            Inside.Value = true;
            await navigation();

            // Stamped only on a transition that actually completed. One that threw left the stack
            // where it was, and the recovery navigation a caller makes from its catch block must
            // not then be dropped as though it were the second half of a double-tap.
            _lastCompletedUtc = DateTime.UtcNow;
            return true;
        }
        finally
        {
            Inside.Value = false;
            Interlocked.Exchange(ref _inFlight, 0);
        }
    }
}
