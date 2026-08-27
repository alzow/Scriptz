namespace QueueApp.Features.BusinessDetail;

public partial class BusinessDetailPage : ContentPage
{
    public BusinessDetailPage()
    {
        InitializeComponent();
    }

    // Android's hardware back has to match the on-screen one — without this it pops the whole page
    // from step three instead of stepping back through the flow.
    protected override bool OnBackButtonPressed()
    {
        return BindingContext is BusinessDetailPageViewModel vm && vm.TryHandleHardwareBack()
            || base.OnBackButtonPressed();
    }
}
