using System.Windows.Input;

namespace QueueApp.Features.History.Helpers;

public partial class HistoryFilterBar : ContentView
{
    public static readonly BindableProperty IsAllSelectedProperty = BindableProperty.Create(
        nameof(IsAllSelected), typeof(bool), typeof(HistoryFilterBar), default(bool));
    public static readonly BindableProperty IsVisitsSelectedProperty = BindableProperty.Create(
        nameof(IsVisitsSelected), typeof(bool), typeof(HistoryFilterBar), default(bool));
    public static readonly BindableProperty IsBookingsSelectedProperty = BindableProperty.Create(
        nameof(IsBookingsSelected), typeof(bool), typeof(HistoryFilterBar), default(bool));
    public static readonly BindableProperty SetFilterCommandProperty = BindableProperty.Create(
        nameof(SetFilterCommand), typeof(ICommand), typeof(HistoryFilterBar), default(ICommand));

    public bool IsAllSelected
    {
        get => (bool)GetValue(IsAllSelectedProperty);
        set => SetValue(IsAllSelectedProperty, value);
    }

    public bool IsVisitsSelected
    {
        get => (bool)GetValue(IsVisitsSelectedProperty);
        set => SetValue(IsVisitsSelectedProperty, value);
    }

    public bool IsBookingsSelected
    {
        get => (bool)GetValue(IsBookingsSelectedProperty);
        set => SetValue(IsBookingsSelectedProperty, value);
    }

    public ICommand SetFilterCommand
    {
        get => (ICommand)GetValue(SetFilterCommandProperty);
        set => SetValue(SetFilterCommandProperty, value);
    }

    public HistoryFilterBar()
    {
        InitializeComponent();
    }
}
