using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class LiveQueueSummaryView : ContentView
{
    public static readonly BindableProperty QueueSummaryProperty = BindableProperty.Create(
        nameof(QueueSummary), typeof(IEnumerable), typeof(LiveQueueSummaryView), default(IEnumerable));
    public static readonly BindableProperty JoinCommandProperty = BindableProperty.Create(
        nameof(JoinCommand), typeof(ICommand), typeof(LiveQueueSummaryView), default(ICommand));

    public IEnumerable QueueSummary
    {
        get => (IEnumerable)GetValue(QueueSummaryProperty);
        set => SetValue(QueueSummaryProperty, value);
    }

    public ICommand JoinCommand
    {
        get => (ICommand)GetValue(JoinCommandProperty);
        set => SetValue(JoinCommandProperty, value);
    }

    public LiveQueueSummaryView()
    {
        InitializeComponent();
    }
}
