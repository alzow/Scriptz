namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeShortTextFieldView : ContentView
{
    // Two-way by default: the answer typed here is the flow's state, not a copy of it.
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(IntakeShortTextFieldView), string.Empty,
        BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(IntakeShortTextFieldView), "Type your answer");

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public IntakeShortTextFieldView()
    {
        InitializeComponent();
    }
}
