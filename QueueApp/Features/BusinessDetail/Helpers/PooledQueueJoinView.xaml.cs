using System.Windows.Input;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class PooledQueueJoinView : ContentView
{
    public static readonly BindableProperty WaitingCountProperty = BindableProperty.Create(
        nameof(WaitingCount), typeof(int), typeof(PooledQueueJoinView), default(int));
    public static readonly BindableProperty WaitMinutesProperty = BindableProperty.Create(
        nameof(WaitMinutes), typeof(double), typeof(PooledQueueJoinView), default(double));
    public static readonly BindableProperty JoinCommandProperty = BindableProperty.Create(
        nameof(JoinCommand), typeof(ICommand), typeof(PooledQueueJoinView), default(ICommand));
    public static readonly BindableProperty IsJoiningProperty = BindableProperty.Create(
        nameof(IsJoining), typeof(bool), typeof(PooledQueueJoinView), default(bool));

    public int WaitingCount
    {
        get => (int)GetValue(WaitingCountProperty);
        set => SetValue(WaitingCountProperty, value);
    }

    public double WaitMinutes
    {
        get => (double)GetValue(WaitMinutesProperty);
        set => SetValue(WaitMinutesProperty, value);
    }

    public ICommand JoinCommand
    {
        get => (ICommand)GetValue(JoinCommandProperty);
        set => SetValue(JoinCommandProperty, value);
    }

    public bool IsJoining
    {
        get => (bool)GetValue(IsJoiningProperty);
        set => SetValue(IsJoiningProperty, value);
    }

    public PooledQueueJoinView()
    {
        InitializeComponent();
    }
}
