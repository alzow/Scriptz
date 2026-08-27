using System.Windows.Input;

namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class ShopHeaderView : ContentView
{
    public static readonly BindableProperty BusinessNameProperty = BindableProperty.Create(
        nameof(BusinessName), typeof(string), typeof(ShopHeaderView), string.Empty);

    public static readonly BindableProperty IsLiveProperty = BindableProperty.Create(
        nameof(IsLive), typeof(bool), typeof(ShopHeaderView), false);

    public static readonly BindableProperty OpenSettingsCommandProperty = BindableProperty.Create(
        nameof(OpenSettingsCommand), typeof(ICommand), typeof(ShopHeaderView));

    public string BusinessName
    {
        get => (string)GetValue(BusinessNameProperty);
        set => SetValue(BusinessNameProperty, value);
    }

    public bool IsLive
    {
        get => (bool)GetValue(IsLiveProperty);
        set => SetValue(IsLiveProperty, value);
    }

    public ICommand? OpenSettingsCommand
    {
        get => (ICommand?)GetValue(OpenSettingsCommandProperty);
        set => SetValue(OpenSettingsCommandProperty, value);
    }

    public ShopHeaderView()
    {
        InitializeComponent();
    }
}
