using System.Windows.Input;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Auth.Helpers;

public partial class RegisterFormView : ContentView
{
    public static readonly BindableProperty DisplayNameProperty = BindableProperty.Create(
        nameof(DisplayName), typeof(string), typeof(RegisterFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty EmailAddressProperty = BindableProperty.Create(
        nameof(EmailAddress), typeof(string), typeof(RegisterFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PhoneNumberProperty = BindableProperty.Create(
        nameof(PhoneNumber), typeof(string), typeof(RegisterFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PasswordProperty = BindableProperty.Create(
        nameof(Password), typeof(string), typeof(RegisterFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty ConfirmPasswordProperty = BindableProperty.Create(
        nameof(ConfirmPassword), typeof(string), typeof(RegisterFormView), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty NameValidatorProperty = BindableProperty.Create(
        nameof(NameValidator), typeof(IValidator), typeof(RegisterFormView));

    public static readonly BindableProperty EmailValidatorProperty = BindableProperty.Create(
        nameof(EmailValidator), typeof(IValidator), typeof(RegisterFormView));

    public static readonly BindableProperty PhoneValidatorProperty = BindableProperty.Create(
        nameof(PhoneValidator), typeof(IValidator), typeof(RegisterFormView));

    public static readonly BindableProperty PasswordValidatorProperty = BindableProperty.Create(
        nameof(PasswordValidator), typeof(IValidator), typeof(RegisterFormView));

    public static readonly BindableProperty FormStateManagerProperty = BindableProperty.Create(
        nameof(FormStateManager), typeof(ISharedStateManager), typeof(RegisterFormView));

    public static readonly BindableProperty ShowPasswordMismatchProperty = BindableProperty.Create(
        nameof(ShowPasswordMismatch), typeof(bool), typeof(RegisterFormView), false);

    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(RegisterFormView), string.Empty);

    public static readonly BindableProperty IsRegisteringProperty = BindableProperty.Create(
        nameof(IsRegistering), typeof(bool), typeof(RegisterFormView), false, BindingMode.TwoWay);

    public static readonly BindableProperty CanSubmitProperty = BindableProperty.Create(
        nameof(CanSubmit), typeof(bool), typeof(RegisterFormView), false);

    public static readonly BindableProperty RegisterCommandProperty = BindableProperty.Create(
        nameof(RegisterCommand), typeof(ICommand), typeof(RegisterFormView));

    public static readonly BindableProperty PasswordRuleTextProperty = BindableProperty.Create(
        nameof(PasswordRuleText), typeof(string), typeof(RegisterFormView), string.Empty);

    public static readonly BindableProperty TermsTextProperty = BindableProperty.Create(
        nameof(TermsText), typeof(string), typeof(RegisterFormView), string.Empty);

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string EmailAddress
    {
        get => (string)GetValue(EmailAddressProperty);
        set => SetValue(EmailAddressProperty, value);
    }

    public string PhoneNumber
    {
        get => (string)GetValue(PhoneNumberProperty);
        set => SetValue(PhoneNumberProperty, value);
    }

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public string ConfirmPassword
    {
        get => (string)GetValue(ConfirmPasswordProperty);
        set => SetValue(ConfirmPasswordProperty, value);
    }

    public IValidator? NameValidator
    {
        get => (IValidator?)GetValue(NameValidatorProperty);
        set => SetValue(NameValidatorProperty, value);
    }

    public IValidator? EmailValidator
    {
        get => (IValidator?)GetValue(EmailValidatorProperty);
        set => SetValue(EmailValidatorProperty, value);
    }

    public IValidator? PhoneValidator
    {
        get => (IValidator?)GetValue(PhoneValidatorProperty);
        set => SetValue(PhoneValidatorProperty, value);
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

    public bool ShowPasswordMismatch
    {
        get => (bool)GetValue(ShowPasswordMismatchProperty);
        set => SetValue(ShowPasswordMismatchProperty, value);
    }

    public string ErrorMessage
    {
        get => (string)GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    public bool IsRegistering
    {
        get => (bool)GetValue(IsRegisteringProperty);
        set => SetValue(IsRegisteringProperty, value);
    }

    public bool CanSubmit
    {
        get => (bool)GetValue(CanSubmitProperty);
        set => SetValue(CanSubmitProperty, value);
    }

    public ICommand? RegisterCommand
    {
        get => (ICommand?)GetValue(RegisterCommandProperty);
        set => SetValue(RegisterCommandProperty, value);
    }

    public string PasswordRuleText
    {
        get => (string)GetValue(PasswordRuleTextProperty);
        set => SetValue(PasswordRuleTextProperty, value);
    }

    public string TermsText
    {
        get => (string)GetValue(TermsTextProperty);
        set => SetValue(TermsTextProperty, value);
    }

    public RegisterFormView()
    {
        InitializeComponent();
    }
}
