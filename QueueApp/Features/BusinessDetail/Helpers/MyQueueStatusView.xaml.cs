using System.Windows.Input;
using QueueApp.Services.Api.Queue.Models;

namespace QueueApp.Features.BusinessDetail.Helpers;

public partial class MyQueueStatusView : ContentView
{
    public static readonly BindableProperty MyStatusProperty = BindableProperty.Create(
        nameof(MyStatus), typeof(MyQueueStatusResponse), typeof(MyQueueStatusView), default(MyQueueStatusResponse));
    public static readonly BindableProperty MyWaitMinutesProperty = BindableProperty.Create(
        nameof(MyWaitMinutes), typeof(decimal?), typeof(MyQueueStatusView), default(decimal?));
    public static readonly BindableProperty IsBeingServedProperty = BindableProperty.Create(
        nameof(IsBeingServed), typeof(bool), typeof(MyQueueStatusView), default(bool));
    public static readonly BindableProperty LeaveCommandProperty = BindableProperty.Create(
        nameof(LeaveCommand), typeof(ICommand), typeof(MyQueueStatusView), default(ICommand));
    public static readonly BindableProperty IsLeavingProperty = BindableProperty.Create(
        nameof(IsLeaving), typeof(bool), typeof(MyQueueStatusView), default(bool));

    public MyQueueStatusResponse MyStatus
    {
        get => (MyQueueStatusResponse)GetValue(MyStatusProperty);
        set => SetValue(MyStatusProperty, value);
    }

    public decimal? MyWaitMinutes
    {
        get => (decimal?)GetValue(MyWaitMinutesProperty);
        set => SetValue(MyWaitMinutesProperty, value);
    }

    public bool IsBeingServed
    {
        get => (bool)GetValue(IsBeingServedProperty);
        set => SetValue(IsBeingServedProperty, value);
    }

    public ICommand LeaveCommand
    {
        get => (ICommand)GetValue(LeaveCommandProperty);
        set => SetValue(LeaveCommandProperty, value);
    }

    public bool IsLeaving
    {
        get => (bool)GetValue(IsLeavingProperty);
        set => SetValue(IsLeavingProperty, value);
    }

    public MyQueueStatusView()
    {
        InitializeComponent();
    }
}
