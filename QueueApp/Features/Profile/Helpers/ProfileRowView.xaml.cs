using System.Windows.Input;

namespace QueueApp.Features.Profile.Helpers;

public partial class ProfileRowView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(ProfileRowView), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty DetailProperty = BindableProperty.Create(
        nameof(Detail), typeof(string), typeof(ProfileRowView), string.Empty);

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ProfileRowView));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty ShowChevronProperty = BindableProperty.Create(
        nameof(ShowChevron), typeof(bool), typeof(ProfileRowView), true);

    public bool ShowChevron
    {
        get => (bool)GetValue(ShowChevronProperty);
        set => SetValue(ShowChevronProperty, value);
    }

    public static readonly BindableProperty TitleStyleProperty = BindableProperty.Create(
        nameof(TitleStyle), typeof(Style), typeof(ProfileRowView), null,
        defaultValueCreator: _ => Application.Current?.Resources["lbl15_Bold_TextDark"] as Style);

    public Style TitleStyle
    {
        get => (Style)GetValue(TitleStyleProperty);
        set => SetValue(TitleStyleProperty, value);
    }

    public ProfileRowView()
    {
        InitializeComponent();
    }
}
