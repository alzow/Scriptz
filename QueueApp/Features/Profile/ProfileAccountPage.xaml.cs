using QueueApp.Framework.Navigation;

namespace QueueApp.Features.Profile;

public partial class ProfileAccountPage : ContentPage, ISystemBackButtonClickAware
{
    public ProfileAccountPage()
    {
        InitializeComponent();
    }

    // This page is the root of its own modal NavigationPage, so there is no page under it for the
    // framework's pop to land on — its back is the dismissal its header chevron performs.
    public bool OnSystemBackButtonClick() => SystemBackHandler.TryHandle(BindingContext);
}
