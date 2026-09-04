using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Features.Auth.Constants;
using QueueApp.Features.Auth.Helpers;
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
    public string HeadingText => AuthConstants.SignUpHeading;
    public string LeadText => AuthConstants.SignUpLead;

    public string PasswordRuleText => $"At least {AuthConstants.PasswordMinimumLength} characters.";

    // TODO: link the two phrases once SupportLinks carries the terms and privacy URLs. A quiet
    // line rather than a checkbox: a checkbox implies a choice that is not on offer.
    public string TermsText => AuthConstants.Terms;

    public ISharedStateManager FormStateManager { get; } = new FormValidators.SharedStateManager();

    public IValidator NameValidator { get; } = new FormValidators.RequiredValidator("Enter your name.");
    public IValidator EmailValidator { get; } = new FormValidators.EmailValidator("Enter a valid email address.");
    public IValidator PhoneValidator { get; } = new FormValidators.SaPhoneValidator("Enter a valid SA mobile number.");
    public IValidator PasswordValidator { get; } = new FormValidators.MinLengthValidator(
        AuthConstants.PasswordMinimumLength, $"At least {AuthConstants.PasswordMinimumLength} characters.");

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

    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;

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
                ErrorMessage = AuthConstants.PhoneTakenMessage;
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

            await _popupService.ShowAlertAsync("Almost there", AuthConstants.ConfirmEmailMessage);
            await NavigationService.NavigateAsync(NavigationPaths.Login);
        }
        catch (ApiException exception)
        {
            ErrorMessage = AuthHelper.TranslateSignUpError(exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = AuthConstants.OfflineMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = AuthConstants.SignUpFailureMessage;
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsRegistering = false;
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
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
}
