using System.Windows.Input;

namespace QueueApp.Features.Settings.Helpers;

public partial class SettingsHeaderView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(SettingsHeaderView), string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty ShowCloseProperty = BindableProperty.Create(
        nameof(ShowClose), typeof(bool), typeof(SettingsHeaderView), false);

    public bool ShowClose
    {
        get => (bool)GetValue(ShowCloseProperty);
        set => SetValue(ShowCloseProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(SettingsHeaderView));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public SettingsHeaderView()
    {
        InitializeComponent();
    }
}
