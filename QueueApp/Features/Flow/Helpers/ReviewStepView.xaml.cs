namespace QueueApp.Features.Flow.Helpers;

public partial class ReviewStepView : ContentView
{
    public static readonly BindableProperty ShowReviewStepProperty = BindableProperty.Create(
        nameof(ShowReviewStep), typeof(bool), typeof(ReviewStepView), false);

    public static readonly BindableProperty ReviewOperatorLabelProperty = BindableProperty.Create(
        nameof(ReviewOperatorLabel), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewOperatorTextProperty = BindableProperty.Create(
        nameof(ReviewOperatorText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewServiceTextProperty = BindableProperty.Create(
        nameof(ReviewServiceText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewPriceTextProperty = BindableProperty.Create(
        nameof(ReviewPriceText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewPositionTextProperty = BindableProperty.Create(
        nameof(ReviewPositionText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewTurnTextProperty = BindableProperty.Create(
        nameof(ReviewTurnText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ReviewWhenTextProperty = BindableProperty.Create(
        nameof(ReviewWhenText), typeof(string), typeof(ReviewStepView), string.Empty);

    public static readonly BindableProperty ShowReviewWhenProperty = BindableProperty.Create(
        nameof(ShowReviewWhen), typeof(bool), typeof(ReviewStepView), false);

    public static readonly BindableProperty ShowReviewQueueLinesProperty = BindableProperty.Create(
        nameof(ShowReviewQueueLines), typeof(bool), typeof(ReviewStepView), true);

    public static readonly BindableProperty BookingNoteProperty = BindableProperty.Create(
        nameof(BookingNote), typeof(string), typeof(ReviewStepView), string.Empty,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty NoteLabelTextProperty = BindableProperty.Create(
        nameof(NoteLabelText), typeof(string), typeof(ReviewStepView), "ANYTHING THEY SHOULD KNOW — OPTIONAL");

    public static readonly BindableProperty ShowCustomerCaptureProperty = BindableProperty.Create(
        nameof(ShowCustomerCapture), typeof(bool), typeof(ReviewStepView), false);

    public static readonly BindableProperty CustomerNameProperty = BindableProperty.Create(
        nameof(CustomerName), typeof(string), typeof(ReviewStepView), string.Empty,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty CustomerPhoneProperty = BindableProperty.Create(
        nameof(CustomerPhone), typeof(string), typeof(ReviewStepView), string.Empty,
        defaultBindingMode: BindingMode.TwoWay);

    public bool ShowReviewStep
    {
        get => (bool)GetValue(ShowReviewStepProperty);
        set => SetValue(ShowReviewStepProperty, value);
    }

    public string ReviewOperatorLabel
    {
        get => (string)GetValue(ReviewOperatorLabelProperty);
        set => SetValue(ReviewOperatorLabelProperty, value);
    }

    public string ReviewOperatorText
    {
        get => (string)GetValue(ReviewOperatorTextProperty);
        set => SetValue(ReviewOperatorTextProperty, value);
    }

    public string ReviewServiceText
    {
        get => (string)GetValue(ReviewServiceTextProperty);
        set => SetValue(ReviewServiceTextProperty, value);
    }

    public string ReviewPriceText
    {
        get => (string)GetValue(ReviewPriceTextProperty);
        set => SetValue(ReviewPriceTextProperty, value);
    }

    public string ReviewPositionText
    {
        get => (string)GetValue(ReviewPositionTextProperty);
        set => SetValue(ReviewPositionTextProperty, value);
    }

    public string ReviewWhenText
    {
        get => (string)GetValue(ReviewWhenTextProperty);
        set => SetValue(ReviewWhenTextProperty, value);
    }

    public bool ShowReviewWhen
    {
        get => (bool)GetValue(ShowReviewWhenProperty);
        set => SetValue(ShowReviewWhenProperty, value);
    }

    public bool ShowReviewQueueLines
    {
        get => (bool)GetValue(ShowReviewQueueLinesProperty);
        set => SetValue(ShowReviewQueueLinesProperty, value);
    }

    public string BookingNote
    {
        get => (string)GetValue(BookingNoteProperty);
        set => SetValue(BookingNoteProperty, value);
    }

    public string ReviewTurnText
    {
        get => (string)GetValue(ReviewTurnTextProperty);
        set => SetValue(ReviewTurnTextProperty, value);
    }

    public string NoteLabelText
    {
        get => (string)GetValue(NoteLabelTextProperty);
        set => SetValue(NoteLabelTextProperty, value);
    }

    public bool ShowCustomerCapture
    {
        get => (bool)GetValue(ShowCustomerCaptureProperty);
        set => SetValue(ShowCustomerCaptureProperty, value);
    }

    public string CustomerName
    {
        get => (string)GetValue(CustomerNameProperty);
        set => SetValue(CustomerNameProperty, value);
    }

    public string CustomerPhone
    {
        get => (string)GetValue(CustomerPhoneProperty);
        set => SetValue(CustomerPhoneProperty, value);
    }

    public ReviewStepView()
    {
        InitializeComponent();
    }
}
