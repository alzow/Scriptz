using System.Windows.Input;

namespace QueueApp.Features.BusinessSettings.Helpers;

public partial class GhostAddRowView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(GhostAddRowView), string.Empty);

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(GhostAddRowView));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public GhostAddRowView()
    {
        InitializeComponent();
    }
}
