namespace QueueApp.Services.Onboarding;

// Preferences.Default rather than secure storage: this is a "has this phone ever been past the
// welcome screen" flag, not a credential, and it must survive a sign-out — someone who signs out
// goes back to sign-in, never to the pitch they have already read.
public class FirstRunService : IFirstRunService
{
    private const string WelcomeSeenKey = "onboarding_welcome_seen";

    public bool HasSeenWelcome
    {
        get
        {
            try
            {
                return Preferences.Default.Get(WelcomeSeenKey, false);
            }
            catch (Exception)
            {
                // A phone that cannot read preferences must not be trapped on the welcome screen
                // forever, so an unreadable flag counts as seen.
                return true;
            }
        }
    }

    public void MarkWelcomeSeen()
    {
        try
        {
            Preferences.Default.Set(WelcomeSeenKey, true);
        }
        catch (Exception)
        {
            // Nothing to recover: the welcome screen showing twice is a far smaller problem than
            // a crash on the first screen of the app.
        }
    }
}
