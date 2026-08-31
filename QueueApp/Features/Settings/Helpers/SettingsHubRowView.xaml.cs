using System.Windows.Input;

namespace QueueApp.Features.Settings.Helpers;

public partial class SettingsHubRowView : ContentView
{
    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(SettingsHubRowView), string.Empty);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(SettingsHubRowView), string.Empty);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty ValueTextProperty = BindableProperty.Create(
        nameof(ValueText), typeof(string), typeof(SettingsHubRowView), string.Empty);

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public static readonly BindableProperty IsValueMissingProperty = BindableProperty.Create(
        nameof(IsValueMissing), typeof(bool), typeof(SettingsHubRowView), false);

    public bool IsValueMissing
    {
        get => (bool)GetValue(IsValueMissingProperty);
        set => SetValue(IsValueMissingProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(SettingsHubRowView));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public SettingsHubRowView()
    {
        InitializeComponent();
    }
}
