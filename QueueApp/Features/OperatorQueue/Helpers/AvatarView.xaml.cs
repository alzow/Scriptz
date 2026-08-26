namespace QueueApp.Features.OperatorQueue.Helpers;

public partial class AvatarView : ContentView
{
    public static readonly BindableProperty InitialsProperty = BindableProperty.Create(
        nameof(Initials), typeof(string), typeof(AvatarView), string.Empty);

    public static readonly BindableProperty ShowDotProperty = BindableProperty.Create(
        nameof(ShowDot), typeof(bool), typeof(AvatarView), false);

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(AvatarView), 40d);

    public string Initials
    {
        get => (string)GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public bool ShowDot
    {
        get => (bool)GetValue(ShowDotProperty);
        set => SetValue(ShowDotProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public AvatarView()
    {
        InitializeComponent();
    }
}
