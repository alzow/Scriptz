namespace QueueApp.Features.BusinessDetail.Helpers;

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

    public string ReviewTurnText
    {
        get => (string)GetValue(ReviewTurnTextProperty);
        set => SetValue(ReviewTurnTextProperty, value);
    }

    public ReviewStepView()
    {
        InitializeComponent();
    }
}
