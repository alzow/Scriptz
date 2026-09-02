#if ANDROID
using Android.Provider;
#elif IOS || MACCATALYST
using UIKit;
#endif

namespace QueueApp.Shared.Templates.Skeleton;

public static class ReduceMotion
{
    public static bool IsEnabled()
    {
        try
        {
#if ANDROID
            var resolver = Android.App.Application.Context?.ContentResolver;
            if (resolver is null)
                return false;

            return Settings.Global.GetFloat(resolver, Settings.Global.AnimatorDurationScale, 1f) == 0f;
#elif IOS || MACCATALYST
            return UIAccessibility.IsReduceMotionEnabled;
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
