using QueueApp.Framework.Theming;

namespace QueueApp.Shared.Templates;

/// <summary>
/// An <see cref="Image"/> that carries an icon <em>name</em> rather than a source, and resolves it
/// against the live theme. Pages name the icon; they never name a file or a colour.
/// </summary>
public class ThemedIcon : Image
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(ThemedIcon), string.Empty,
        propertyChanged: (bindable, _, _) => ((ThemedIcon)bindable).ApplySource());

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Subscribed only while realised. These live inside recycled list cells, and a static event
        // holding a strong reference to every cell that ever existed is a leak.
        ThemeService.ThemeChanged -= OnThemeChanged;

        if (Handler is null)
            return;

        ThemeService.ThemeChanged += OnThemeChanged;
        ApplySource();
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        MainThread.BeginInvokeOnMainThread(ApplySource);

    private void ApplySource() => Source = ThemedIcons.Resolve(Icon);
}

/// <summary>The same, for a tappable icon.</summary>
public class ThemedIconButton : ImageButton
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(ThemedIconButton), string.Empty,
        propertyChanged: (bindable, _, _) => ((ThemedIconButton)bindable).ApplySource());

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        ThemeService.ThemeChanged -= OnThemeChanged;

        if (Handler is null)
            return;

        ThemeService.ThemeChanged += OnThemeChanged;
        ApplySource();
    }

    private void OnThemeChanged(object? sender, AppTheme theme) =>
        MainThread.BeginInvokeOnMainThread(ApplySource);

    private void ApplySource() => Source = ThemedIcons.Resolve(Icon);
}
