using System.Windows.Input;

namespace QueueApp.Features.History.Helpers;

public partial class HistoryEmptyStateView : ContentView
{
    public static readonly BindableProperty EmptyTitleProperty = BindableProperty.Create(
        nameof(EmptyTitle), typeof(string), typeof(HistoryEmptyStateView), default(string));
    public static readonly BindableProperty EmptyBodyProperty = BindableProperty.Create(
        nameof(EmptyBody), typeof(string), typeof(HistoryEmptyStateView), default(string));
    public static readonly BindableProperty BrowseCommandProperty = BindableProperty.Create(
        nameof(BrowseCommand), typeof(ICommand), typeof(HistoryEmptyStateView), default(ICommand));

    public string EmptyTitle
    {
        get => (string)GetValue(EmptyTitleProperty);
        set => SetValue(EmptyTitleProperty, value);
    }

    public string EmptyBody
    {
        get => (string)GetValue(EmptyBodyProperty);
        set => SetValue(EmptyBodyProperty, value);
    }

    public ICommand BrowseCommand
    {
        get => (ICommand)GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    public HistoryEmptyStateView()
    {
        InitializeComponent();
    }
}
