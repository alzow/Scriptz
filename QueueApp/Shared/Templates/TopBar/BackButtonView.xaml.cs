using System.Windows.Input;

namespace QueueApp.Shared.Templates.TopBar;

/// <summary>
/// The one back affordance in the app: a chevron on a surface tile. Shared by the queue and booking
/// flows' top bar and by every pushed sub-page header, so back looks and sits the same everywhere.
/// </summary>
public partial class BackButtonView : ContentView
{
    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(BackButtonView));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public BackButtonView()
    {
        InitializeComponent();
    }
}
