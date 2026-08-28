namespace QueueApp.Features.Flow.Confirmation;

public partial class ConfirmationPage : ContentPage
{
    public ConfirmationPage()
    {
        InitializeComponent();
    }

    // There is nothing behind this page to pop to — the submit that reached it replaced the stack —
    // so hardware back has to take the same route out as the on-screen one.
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is not ConfirmationPageViewModel vm)
            return base.OnBackButtonPressed();

        vm.DoneCommand.Execute(null);
        return true;
    }
}
