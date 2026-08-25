using System.Windows.Input;

namespace QueueApp.Features.History.Helpers;

public partial class HistoryRowView : ContentView
{
    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(HistoryRowView), default(ICommand));

    public ICommand OpenCommand
    {
        get => (ICommand)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public HistoryRowView()
    {
        InitializeComponent();
    }
}
