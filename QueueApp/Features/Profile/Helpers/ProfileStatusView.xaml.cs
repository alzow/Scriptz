using System.Windows.Input;

namespace QueueApp.Features.Profile.Helpers;

public partial class ProfileStatusView : ContentView
{
    public static readonly BindableProperty IsAllowedProperty = BindableProperty.Create(
        nameof(IsAllowed), typeof(bool), typeof(ProfileStatusView), true);

    public bool IsAllowed
    {
        get => (bool)GetValue(IsAllowedProperty);
        set => SetValue(IsAllowedProperty, value);
    }

    public static readonly BindableProperty HeadlineProperty = BindableProperty.Create(
        nameof(Headline), typeof(string), typeof(ProfileStatusView), string.Empty);

    public string Headline
    {
        get => (string)GetValue(HeadlineProperty);
        set => SetValue(HeadlineProperty, value);
    }

    public static readonly BindableProperty DetailProperty = BindableProperty.Create(
        nameof(Detail), typeof(string), typeof(ProfileStatusView), string.Empty);

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(ProfileStatusView), string.Empty);

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand), typeof(ICommand), typeof(ProfileStatusView));

    public ICommand ActionCommand
    {
        get => (ICommand)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public ProfileStatusView()
    {
        InitializeComponent();
    }
}
