using System.Windows.Input;

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
        nameof(BackIcon), typeof(string), typeof(AlzowSubPageHeader), "left_arrow");

    public string BackIcon
    {
        get => (string)GetValue(BackIconProperty);
        private set => SetValue(BackIconProperty, value);
    }

    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), typeof(Color), typeof(AlzowSubPageHeader), Colors.Black);

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

    private void ApplyTheme(bool isDark)
    {
        if (isDark)
        {
            BackIcon = "left_arrow_white";
            TitleColor = (Color)Application.Current!.Resources["Cream"];
        }
        else
        {
            BackIcon = "left_arrow";
            TitleColor = (Color)Application.Current!.Resources["TextDark"];
        }
    }

    public AlzowSubPageHeader()
    {
        InitializeComponent();
        ApplyTheme(IsDark);
    }
}
