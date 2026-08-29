using System.Windows.Input;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Auth.Helpers;

public partial class LoginFormView : ContentView
{
    public static readonly BindableProperty EmailAddressProperty = BindableProperty.Create(
        nameof(EmailAddress), typeof(string), typeof(LoginFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PasswordProperty = BindableProperty.Create(
        nameof(Password), typeof(string), typeof(LoginFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty EmailValidatorProperty = BindableProperty.Create(
        nameof(EmailValidator), typeof(IValidator), typeof(LoginFormView));

    public static readonly BindableProperty PasswordValidatorProperty = BindableProperty.Create(
        nameof(PasswordValidator), typeof(IValidator), typeof(LoginFormView));

    public static readonly BindableProperty FormStateManagerProperty = BindableProperty.Create(
        nameof(FormStateManager), typeof(ISharedStateManager), typeof(LoginFormView));

    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(LoginFormView), string.Empty);

    public static readonly BindableProperty IsSigningInProperty = BindableProperty.Create(
        nameof(IsSigningIn), typeof(bool), typeof(LoginFormView), false, BindingMode.TwoWay);

    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(
        nameof(CanSubmit), typeof(bool), typeof(LoginFormView), false);

    public static readonly BindableProperty SignInCommandProperty = BindableProperty.Create(
        nameof(SignInCommand), typeof(ICommand), typeof(LoginFormView));

    public string EmailAddress
    {
        get => (string)GetValue(EmailAddressProperty);
        set => SetValue(EmailAddressProperty, value);
    }

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public IValidator? EmailValidator
    {
        get => (IValidator?)GetValue(EmailValidatorProperty);
        set => SetValue(EmailValidatorProperty, value);
    }

    public IValidator? PasswordValidator
    {
        get => (IValidator?)GetValue(PasswordValidatorProperty);
        set => SetValue(PasswordValidatorProperty, value);
    }

    public ISharedStateManager? FormStateManager
    {
        get => (ISharedStateManager?)GetValue(FormStateManagerProperty);
        set => SetValue(FormStateManagerProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public bool IsSigningIn
    {
        get => (bool)GetValue(IsSigningInProperty);
        set => SetValue(IsSigningInProperty, value);
    }

    public bool CanSubmit
    {
        get => (bool)GetValue(CanSubmitProperty);
        set => SetValue(CanSubmitProperty, value);
    }

    public ICommand? SignInCommand
    {
        get => (ICommand?)GetValue(SignInCommandProperty);
        set => SetValue(SignInCommandProperty, value);
    }

    public LoginFormView()
    {
        InitializeComponent();
    }
}
