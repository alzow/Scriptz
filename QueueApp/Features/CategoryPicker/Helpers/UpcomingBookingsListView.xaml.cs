using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class UpcomingBookingsListView : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IEnumerable), typeof(UpcomingBookingsListView), default(IEnumerable));
    public static readonly BindableProperty CancelCommandProperty = BindableProperty.Create(
        nameof(CancelCommand), typeof(ICommand), typeof(UpcomingBookingsListView), default(ICommand));
    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(UpcomingBookingsListView), default(ICommand));

    public IEnumerable Items
    {
        get => (IEnumerable)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand OpenCommand
    {
        get => (ICommand)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public UpcomingBookingsListView()
    {
        InitializeComponent();
    }
}
