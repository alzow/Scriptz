using System.Collections;
using System.Windows.Input;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class BrowseBusinessListView : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(
        nameof(Items), typeof(IEnumerable), typeof(BrowseBusinessListView), default(IEnumerable));
    public static readonly BindableProperty OpenCommandProperty = BindableProperty.Create(
        nameof(OpenCommand), typeof(ICommand), typeof(BrowseBusinessListView), default(ICommand));

    public IEnumerable Items
    {
        get => (IEnumerable)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ICommand OpenCommand
    {
        get => (ICommand)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public BrowseBusinessListView()
    {
        InitializeComponent();
    }
}
