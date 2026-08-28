using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class BayFilterBarView : ContentView
{
    public static readonly BindableProperty FiltersProperty = BindableProperty.Create(
        nameof(Filters), typeof(IEnumerable), typeof(BayFilterBarView));

    public static readonly BindableProperty SelectFilterCommandProperty = BindableProperty.Create(
        nameof(SelectFilterCommand), typeof(ICommand), typeof(BayFilterBarView));

    public IEnumerable? Filters
    {
        get => (IEnumerable?)GetValue(FiltersProperty);
        set => SetValue(FiltersProperty, value);
    }

    public ICommand? SelectFilterCommand
    {
        get => (ICommand?)GetValue(SelectFilterCommandProperty);
        set => SetValue(SelectFilterCommandProperty, value);
    }

    public BayFilterBarView()
    {
        InitializeComponent();
    }
}
