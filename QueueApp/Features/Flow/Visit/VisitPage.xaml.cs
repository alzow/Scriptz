namespace QueueApp.Features.Flow.Visit;

public partial class VisitPage : ContentPage
{
    public VisitPage()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (BindingContext is VisitPageViewModel vm)
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
