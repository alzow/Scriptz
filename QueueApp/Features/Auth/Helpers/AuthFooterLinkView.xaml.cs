using System.Windows.Input;

namespace QueueApp.Features.Auth.Helpers;

public partial class AuthFooterLinkView : ContentView
{
    public static readonly BindableProperty PromptTextProperty = BindableProperty.Create(
        nameof(PromptText), typeof(string), typeof(AuthFooterLinkView), string.Empty);

    public static readonly BindableProperty ActionTextProperty = BindableProperty.Create(
        nameof(ActionText), typeof(string), typeof(AuthFooterLinkView), string.Empty);

    public static readonly BindableProperty ActionCommandProperty = BindableProperty.Create(
        nameof(ActionCommand), typeof(ICommand), typeof(AuthFooterLinkView));

    public string PromptText
    {
        get => (string)GetValue(PromptTextProperty);
        set => SetValue(PromptTextProperty, value);
    }

    public string ActionText
    {
        get => (string)GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => (ICommand?)GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public AuthFooterLinkView()
    {
        InitializeComponent();
    }
}
