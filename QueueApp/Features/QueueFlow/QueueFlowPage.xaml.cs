using QueueApp.Features.Flow;

namespace QueueApp.Features.QueueFlow;

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
