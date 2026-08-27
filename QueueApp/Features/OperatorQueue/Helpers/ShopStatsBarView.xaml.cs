namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class ShopStatsBarView : ContentView
{
    public static readonly BindableProperty WaitingCountTextProperty = BindableProperty.Create(
        nameof(WaitingCountText), typeof(string), typeof(ShopStatsBarView), string.Empty);

    public static readonly BindableProperty ServingCountTextProperty = BindableProperty.Create(
        nameof(ServingCountText), typeof(string), typeof(ShopStatsBarView), string.Empty);

    public static readonly BindableProperty DoneTodayTextProperty = BindableProperty.Create(
        nameof(DoneTodayText), typeof(string), typeof(ShopStatsBarView), string.Empty);

    public static readonly BindableProperty AvgTextProperty = BindableProperty.Create(
        nameof(AvgText), typeof(string), typeof(ShopStatsBarView), string.Empty);

    public string WaitingCountText
    {
        get => (string)GetValue(WaitingCountTextProperty);
        set => SetValue(WaitingCountTextProperty, value);
    }

    public string ServingCountText
    {
        get => (string)GetValue(ServingCountTextProperty);
        set => SetValue(ServingCountTextProperty, value);
    }

    public string DoneTodayText
    {
        get => (string)GetValue(DoneTodayTextProperty);
        set => SetValue(DoneTodayTextProperty, value);
    }

    public string AvgText
    {
        get => (string)GetValue(AvgTextProperty);
        set => SetValue(AvgTextProperty, value);
    }

    public ShopStatsBarView()
    {
        InitializeComponent();
    }
}
