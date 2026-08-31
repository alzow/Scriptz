namespace QueueApp.Features.Settings;

public partial class WeeklyHoursPage : ContentPage
{
    public WeeklyHoursPage()
    {
        InitializeComponent();
    }

    private void OnDayToggled(object? sender, ToggledEventArgs e)
    {
        if (BindingContext is not WeeklyHoursPageViewModel viewModel)
            return;

        if ((sender as Element)?.BindingContext is not DayGroup day)
            return;

        if (viewModel.ToggleDayOpenCommand.CanExecute(day))
            viewModel.ToggleDayOpenCommand.Execute(day);
    }
}
