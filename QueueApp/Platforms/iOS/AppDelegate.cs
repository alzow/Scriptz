using Foundation;
using QueueApp.Framework.Theming;
using UIKit;

namespace QueueApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		var result = base.FinishedLaunching(application, launchOptions);

		// Keeps the status bar style with the app's theme rather than the phone's, for the case
		// where the operator has pinned one that differs from the system setting.
		PlatformChrome.Start();

		return result;
	}
}
