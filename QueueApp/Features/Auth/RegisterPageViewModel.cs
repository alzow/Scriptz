using System.Net;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;
using FormValidators = QueueApp.Shared.Templates.QueueEntry.Validators;
using Refit;

namespace QueueApp.Features.Auth;

public partial class RegisterPageViewModel : BaseViewModel
{
    #region Constants
    private const int PasswordMinimumLength = 6;

    private const string EmailTakenMessage = "That email is already registered. Try signing in instead.";
    private const string PhoneTakenMessage = "That mobile number is already registered. Try signing in instead.";
    private const string ShortPasswordMessage = "Your password is too short.";
    private const string BadEmailMessage = "That email address doesn't look right.";
    private const string RateLimitedMessage = "Too many attempts. Wait a minute and try again.";
    private const string OfflineMessage = "No connection. Check your internet and try again.";
    private const string GenericFailureMessage = "Couldn't create your account. Please try again.";
    private const string ConfirmEmailMessage = "Account created. Check your email to confirm your address, then sign in.";
    #endregion

    #region Properties
    public ISharedStateManager FormStateManager { get; } = new FormValidators.SharedStateManager();

    public IValidator NameValidator { get; } = new FormValidators.RequiredValidator("Enter your name.");
    public IValidator EmailValidator { get; } = new FormValidators.EmailValidator("Enter a valid email address.");
    public IValidator PhoneValidator { get; } = new FormValidators.SaPhoneValidator("Enter a valid SA mobile number.");
    public IValidator PasswordValidator { get; } = new FormValidators.MinLengthValidator(
        PasswordMinimumLength, $"At least {PasswordMinimumLength} characters.");

    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsFormValid { get; set; }
    public bool IsRegistering { get; set; }

    public bool PasswordsMatch => !string.IsNullOrEmpty(Password) && Password == ConfirmPassword;

    public bool ShowPasswordMismatch => !string.IsNullOrEmpty(ConfirmPassword) && !PasswordsMatch;

    public bool CanSubmit => IsFormValid && PasswordsMatch;
    #endregion

    #region Services
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    #endregion

    #region Constructor
    public RegisterPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;

        FormStateManager.ValidationStateChanged += OnFormValidationStateChanged;
        IsFormValid = FormStateManager.IsValid;
    }
    #endregion

    public void OnFormValidationStateChanged(bool isValid) => IsFormValid = isValid;

    [RelayCommand]
    public async Task RegisterAsync()
    {
        try
        {
            if (!CanSubmit)
                return;

            ErrorMessage = string.Empty;
            IsRegistering = true;

            var phoneAvailable = await _authService.IsPhoneAvailableAsync(Phone.Trim());
            if (!phoneAvailable)
            {
                ErrorMessage = PhoneTakenMessage;
                return;
            }

            var response = await _authService.SignUpAsync(
                Email.Trim(), Password, DisplayName.Trim(), Phone.Trim());

            if (!string.IsNullOrEmpty(response.AccessToken))
            {
                await NavigationService.NavigateAsync(
                    MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: false));
                return;
            }

            await _popupService.ShowAlertAsync("Almost there", ConfirmEmailMessage);
            await NavigationService.GoBackAsync();
        }
        catch (ApiException exception)
        {
            ErrorMessage = TranslateSignUpError(exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = OfflineMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = GenericFailureMessage;
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsRegistering = false;
        }
    }

    [RelayCommand]
    public async Task NavigateToLoginAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public static string TranslateSignUpError(ApiException exception)
    {
        var body = exception.Content ?? string.Empty;

        if (body.Contains("already registered", StringComparison.OrdinalIgnoreCase)
            || body.Contains("already been registered", StringComparison.OrdinalIgnoreCase)
            || body.Contains("user_already_exists", StringComparison.OrdinalIgnoreCase))
            return EmailTakenMessage;

        if (body.Contains("Password should be at least", StringComparison.OrdinalIgnoreCase)
            || body.Contains("weak_password", StringComparison.OrdinalIgnoreCase))
            return ShortPasswordMessage;

        if (body.Contains("invalid format", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Unable to validate email", StringComparison.OrdinalIgnoreCase))
            return BadEmailMessage;

        if (body.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || exception.StatusCode == HttpStatusCode.TooManyRequests)
            return RateLimitedMessage;

        return GenericFailureMessage;
    }
}
