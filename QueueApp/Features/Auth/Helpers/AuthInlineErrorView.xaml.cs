namespace QueueApp.Features.Auth.Helpers;

public partial class AuthInlineErrorView : ContentView
{
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(
        nameof(Message), typeof(string), typeof(AuthInlineErrorView), string.Empty,
        propertyChanged: OnMessageChanged);

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public AuthInlineErrorView()
    {
        InitializeComponent();
    }

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((AuthInlineErrorView)bindable).IsVisible = !string.IsNullOrWhiteSpace(newValue as string);
    }
}
