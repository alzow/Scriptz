namespace QueueApp.Features.Flow.QueueFlow;

public partial class QueueFlowPage : ContentPage
{
    public QueueFlowPage()
    {
        InitializeComponent();
    }

    // A throw out of OnBackButtonPressed takes the app down with it, so the press falls back to the
    // platform's own handling rather than escaping.
    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (BindingContext is FlowPageViewModelBase vm && vm.TryHandleHardwareBack())
                return true;
        }
        catch (Exception)
        {
        }

        return base.OnBackButtonPressed();
    }
}
