using System.Windows.Input;

namespace QueueApp.Features.CategoryPicker.Helpers;

public partial class LocationBarView : ContentView
{
    public static readonly BindableProperty LocationLabelProperty = BindableProperty.Create(
        nameof(LocationLabel), typeof(string), typeof(LocationBarView), default(string));
    public static readonly BindableProperty IsResolvingProperty = BindableProperty.Create(
        nameof(IsResolving), typeof(bool), typeof(LocationBarView), default(bool));
    public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create(
        nameof(RefreshCommand), typeof(ICommand), typeof(LocationBarView), default(ICommand));

    public string LocationLabel
    {
        get => (string)GetValue(LocationLabelProperty);
        set => SetValue(LocationLabelProperty, value);
    }

    public bool IsResolving
    {
        get => (bool)GetValue(IsResolvingProperty);
        set => SetValue(IsResolvingProperty, value);
    }

    public ICommand RefreshCommand
    {
        get => (ICommand)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public LocationBarView()
    {
        InitializeComponent();
    }
}
