
namespace QueueApp.Features.Flow.QueueFlow;

public partial class QueueFlowPage : ContentPage
{
    public QueueFlowPage()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        return BindingContext is FlowPageViewModelBase vm && vm.TryHandleHardwareBack()
            || base.OnBackButtonPressed();
    }
}
