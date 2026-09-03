namespace QueueApp.Features.Flow.Helpers.Intake;

public partial class IntakeLongTextFieldView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(IntakeLongTextFieldView), string.Empty,
        BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(IntakeLongTextFieldView), "Type your answer");

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

    public IntakeLongTextFieldView()
    {
        InitializeComponent();
    }
}
