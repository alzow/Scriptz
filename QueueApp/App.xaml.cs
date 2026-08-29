using QueueApp.Framework.Theming;

namespace QueueApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Before the first page is built: the stored choice has to be on Application.UserAppTheme
        // by the time anything resolves an AppThemeBinding, or the app flashes the wrong theme.
        ThemeService.Initialise(this);
    }
}
