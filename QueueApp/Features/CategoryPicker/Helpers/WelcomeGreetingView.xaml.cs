namespace QueueApp.Features.CategoryPicker.Helpers;

// Claude-new-chat-style greeting. Line 1 is the customer's name (static); line 2 is a random
// saying, typed out character by character on appearing. Bind Name to the customer's display
// name, or null for a generic first line.
public partial class WelcomeGreetingView : ContentView
{
    private static readonly string[] Sayings =
    [
        "The legend returns.",
        "Ready when you are.",
        "Let's find you a spot.",
        "Good to see you again.",
        "Back for more, huh?",
        "No queue too long today.",
        "Let's get you seen.",
        "Straight to the good stuff.",
    ];

    private static readonly Random Rng = new();

    private static readonly TimeSpan CharacterDelay = TimeSpan.FromMilliseconds(28);

    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(WelcomeGreetingView), default(string),
        propertyChanged: OnNameChanged);

    public static readonly BindableProperty NameLineProperty = BindableProperty.Create(
        nameof(NameLine), typeof(string), typeof(WelcomeGreetingView), string.Empty);

    public static readonly BindableProperty SayingProperty = BindableProperty.Create(
        nameof(Saying), typeof(string), typeof(WelcomeGreetingView), string.Empty);

    private uint _animationToken;

    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public string NameLine
    {
        get => (string)GetValue(NameLineProperty);
        private set => SetValue(NameLineProperty, value);
    }

    public string Saying
    {
        get => (string)GetValue(SayingProperty);
        private set => SetValue(SayingProperty, value);
    }

    public WelcomeGreetingView()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
            PlayGreeting();
    }

    private static void OnNameChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is WelcomeGreetingView view && view.Handler is not null)
            view.PlayGreeting();
    }

    private void PlayGreeting()
    {
        NameLine = string.IsNullOrWhiteSpace(Name) ? "Welcome" : $"Hi, {Name}";

        var saying = Sayings[Rng.Next(Sayings.Length)];
        var token = unchecked(++_animationToken);

        Saying = string.Empty;
        var i = 0;
        Dispatcher.StartTimer(CharacterDelay, () =>
        {
            if (token != _animationToken) return false;
            i++;
            Saying = saying[..i];
            return i < saying.Length;
        });
    }
}
