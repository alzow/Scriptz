using System.Windows.Input;

namespace QueueApp.Features.Profile.Helpers;

public partial class LeaveAtChipView : ContentView
{
    public static readonly BindableProperty MinutesProperty = BindableProperty.Create(
        nameof(Minutes), typeof(int), typeof(LeaveAtChipView), 0);

    public int Minutes
    {
        get => (int)GetValue(MinutesProperty);
        set => SetValue(MinutesProperty, value);
    }

    public static readonly BindableProperty SelectedMinutesProperty = BindableProperty.Create(
        nameof(SelectedMinutes), typeof(int), typeof(LeaveAtChipView), 0);

    public int SelectedMinutes
    {
        get => (int)GetValue(SelectedMinutesProperty);
        set => SetValue(SelectedMinutesProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(LeaveAtChipView));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public LeaveAtChipView()
    {
        InitializeComponent();
    }
}
