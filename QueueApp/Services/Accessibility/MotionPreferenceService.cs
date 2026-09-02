namespace QueueApp.Services.Accessibility;

// Read on every access rather than cached: both platforms let the setting change while the app is
// backgrounded, and the only caller asks once per page appearance.
public class MotionPreferenceService : IMotionPreferenceService
{
    public bool PrefersReducedMotion
    {
        get
        {
            try
            {
#if IOS || MACCATALYST
                return UIKit.UIAccessibility.IsReduceMotionEnabled;
#elif ANDROID
                var context = Android.App.Application.Context;
                var resolver = context?.ContentResolver;
                if (resolver is null)
                    return false;

                // Android has no "reduce motion" switch of its own. Developer options' animation
                // scales are what an accessibility guide tells someone to turn off, and a zero
                // scale is the platform saying "do not animate".
                var scale = Android.Provider.Settings.Global.GetFloat(
                    resolver, Android.Provider.Settings.Global.AnimatorDurationScale, 1f);

                return scale == 0f;
#else
                return false;
#endif
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
