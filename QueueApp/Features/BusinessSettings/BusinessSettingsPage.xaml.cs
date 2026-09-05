using QueueApp.Framework.Navigation;

namespace QueueApp.Features.BusinessSettings;

public partial class BusinessSettingsPage : ContentPage, ISystemBackButtonClickAware
{
    public BusinessSettingsPage()
    {
        InitializeComponent();
    }

    // This page is the root of its own modal NavigationPage, so there is no page under it for the
    // framework's pop to land on — its back is the dismissal its header chevron performs.
    public bool OnSystemBackButtonClick() => SystemBackHandler.TryHandle(BindingContext);
}
