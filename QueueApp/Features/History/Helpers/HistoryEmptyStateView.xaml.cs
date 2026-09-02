using System.Windows.Input;

namespace QueueApp.Features.History.Helpers;

public partial class HistoryEmptyStateView : ContentView
{
    public const string DefaultIcon = "ic_ticket";
    public const string DefaultActionText = "Browse nearby";

    public static readonly BindableProperty EmptyTitleProperty = BindableProperty.Create(
        nameof(EmptyTitle), typeof(string), typeof(HistoryEmptyStateView), default(string));
    public static readonly BindableProperty EmptyBodyProperty = BindableProperty.Create(
        nameof(EmptyBody), typeof(string), typeof(HistoryEmptyStateView), default(string));
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(HistoryEmptyStateView), DefaultIcon);
    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(HistoryEmptyStateView), DefaultActionText);
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

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
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
