namespace QueueApp.Features.Flow.Visit.Helpers;

public partial class VisitReasonBlockView : ContentView
{
    public static readonly BindableProperty ReasonTitleProperty = BindableProperty.Create(
        nameof(ReasonTitle), typeof(string), typeof(VisitReasonBlockView), string.Empty);

    public static readonly BindableProperty ReasonBodyProperty = BindableProperty.Create(
        nameof(ReasonBody), typeof(string), typeof(VisitReasonBlockView), string.Empty);

    public static readonly BindableProperty ReasonQuoteProperty = BindableProperty.Create(
        nameof(ReasonQuote), typeof(string), typeof(VisitReasonBlockView), string.Empty);

    public string ReasonTitle
    {
        get => (string)GetValue(ReasonTitleProperty);
        set => SetValue(ReasonTitleProperty, value);
    }

    public string ReasonBody
    {
        get => (string)GetValue(ReasonBodyProperty);
        set => SetValue(ReasonBodyProperty, value);
    }

    public string ReasonQuote
    {
        get => (string)GetValue(ReasonQuoteProperty);
        set => SetValue(ReasonQuoteProperty, value);
    }

    public VisitReasonBlockView()
    {
        InitializeComponent();
    }
}
