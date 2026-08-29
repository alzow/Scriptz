using System.Windows.Input;
using QueueApp.Framework.Theming;

namespace QueueApp.Shared.Templates.AlzowSubPageHeader;

public partial class AlzowSubPageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(AlzowSubPageHeader), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty BackCommandProperty = BindableProperty.Create(
        nameof(BackCommand), typeof(ICommand), typeof(AlzowSubPageHeader));

    public ICommand BackCommand
    {
        get => (ICommand)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    public static readonly BindableProperty IsDarkProperty = BindableProperty.Create(
        nameof(IsDark), typeof(bool), typeof(AlzowSubPageHeader), false,
        propertyChanged: OnIsDarkChanged);

    public bool IsDark
    {
        get => (bool)GetValue(IsDarkProperty);
        set => SetValue(IsDarkProperty, value);
    }

    public static readonly BindableProperty BackIconProperty = BindableProperty.Create(
        nameof(BackIcon), typeof(string), typeof(AlzowSubPageHeader), "left_arrow_white");

    public string BackIcon
    {
        get => (string)GetValue(BackIconProperty);
        private set => SetValue(BackIconProperty, value);
    }

    // Resolved per instance rather than as a static default: a static default would be evaluated
    // before Application.Current exists, and a black title is invisible on the dark theme.
    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), typeof(Color), typeof(AlzowSubPageHeader), null,
        defaultValueCreator: _ => ThemePalette.TextInk);

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        private set => SetValue(TitleColorProperty, value);
    }

    private static void OnIsDarkChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AlzowSubPageHeader header)
            header.ApplyTheme((bool)newValue);
    }

    // Both branches resolved to the same ink even before theming; the header title is TextInk,
    // which the app theme already flips for us.
    private void ApplyTheme(bool isDark)
    {
        BackIcon = "left_arrow_white";
        TitleColor = ThemePalette.TextInk;
    }

    public AlzowSubPageHeader()
    {
        InitializeComponent();
        ApplyTheme(IsDark);
    }
}
