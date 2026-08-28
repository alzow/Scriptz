namespace QueueApp.Features.BookingAgenda.Helpers;

public partial class AgendaStatsBarView : ContentView
{
    public static readonly BindableProperty BookedCountTextProperty = BindableProperty.Create(
        nameof(BookedCountText), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public static readonly BindableProperty FreeTextProperty = BindableProperty.Create(
        nameof(FreeText), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public static readonly BindableProperty RevenueTextProperty = BindableProperty.Create(
        nameof(RevenueText), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public static readonly BindableProperty RevenueLabelProperty = BindableProperty.Create(
        nameof(RevenueLabel), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public static readonly BindableProperty ResourceCountTextProperty = BindableProperty.Create(
        nameof(ResourceCountText), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public static readonly BindableProperty ResourceCountLabelProperty = BindableProperty.Create(
        nameof(ResourceCountLabel), typeof(string), typeof(AgendaStatsBarView), string.Empty);

    public string BookedCountText
    {
        get => (string)GetValue(BookedCountTextProperty);
        set => SetValue(BookedCountTextProperty, value);
    }

    public string FreeText
    {
        get => (string)GetValue(FreeTextProperty);
        set => SetValue(FreeTextProperty, value);
    }

    public string RevenueText
    {
        get => (string)GetValue(RevenueTextProperty);
        set => SetValue(RevenueTextProperty, value);
    }

    public string RevenueLabel
    {
        get => (string)GetValue(RevenueLabelProperty);
        set => SetValue(RevenueLabelProperty, value);
    }

    public string ResourceCountText
    {
        get => (string)GetValue(ResourceCountTextProperty);
        set => SetValue(ResourceCountTextProperty, value);
    }

    public string ResourceCountLabel
    {
        get => (string)GetValue(ResourceCountLabelProperty);
        set => SetValue(ResourceCountLabelProperty, value);
    }

    public AgendaStatsBarView()
    {
        InitializeComponent();
    }
}
