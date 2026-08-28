
namespace QueueApp.Features.Flow.BookingFlow;

public partial class BookingFlowPage : ContentPage
{
    public BookingFlowPage()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        return BindingContext is FlowPageViewModelBase vm && vm.TryHandleHardwareBack()
            || base.OnBackButtonPressed();
    }
}
