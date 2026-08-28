namespace QueueApp.Features.Flow.Confirmation;

public partial class ConfirmationPage : ContentPage
{
    public ConfirmationPage()
    {
        InitializeComponent();
    }

    // There is nothing behind this page to pop to — the submit that reached it replaced the stack —
    // so hardware back has to take the same route out as the on-screen one. A throw here takes the
    // app down, so it falls back to the platform's handling instead.
    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (BindingContext is ConfirmationPageViewModel vm)
            {
                vm.DoneCommand.Execute(null);
                return true;
            }
        }
        catch (Exception)
        {
        }

        return base.OnBackButtonPressed();
    }
}
