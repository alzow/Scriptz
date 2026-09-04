using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Features.Auth.Constants;
using QueueApp.Features.Auth.Helpers;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;
using FormValidators = QueueApp.Shared.Templates.QueueEntry.Validators;
using Refit;

namespace QueueApp.Features.Auth;

public partial class LoginPageViewModel : BaseViewModel
{
    public string HeadingText => AuthConstants.SignInHeading;
    public string LeadText => AuthConstants.SignInLead;

    public bool CanGoBack { get; set; }

    public ISharedStateManager FormStateManager { get; } = new FormValidators.SharedStateManager();

    public IValidator EmailValidator { get; } = new FormValidators.EmailValidator(AuthConstants.InvalidEmailValidation);
    public IValidator PasswordValidator { get; } = new FormValidators.RequiredValidator(AuthConstants.MissingPasswordValidation);

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsFormValid { get; set; }
    public bool IsSigningIn { get; set; }

    public bool CanSubmit => IsFormValid;

    private readonly IAuthService _authService;
    private readonly IBusinessService _businessService;

    public LoginPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _businessService = businessService;

        FormStateManager.ValidationStateChanged += OnFormValidationStateChanged;
        IsFormValid = FormStateManager.IsValid;
    }

    public override void Initialize(INavigationParameters parameters)
    {
        try
        {
            base.Initialize(parameters);

            CanGoBack = parameters is not null
                && parameters.TryGetValue(NavigationKeys.CanGoBack, out var canGoBack)
                && canGoBack is true;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void OnFormValidationStateChanged(bool isValid) => IsFormValid = isValid;

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

    // TODO (Step 5b): swap for Supabase phone-OTP sign-in. Token pipeline stays the same.
    [RelayCommand]
    public async Task LoginAsync()
    {
        try
        {
            if (!CanSubmit)
                return;

            ErrorMessage = string.Empty;
            IsSigningIn = true;

            var response = await _authService.SignInAsync(Email.Trim(), Password);

            if (string.IsNullOrEmpty(response.AccessToken))
            {
                ErrorMessage = InvalidCredentialsMessage;
                return;
            }

            var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
            var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness, manageMode: mode);
            await NavigationService.NavigateAsync(uri);
        }
        catch (ApiException exception)
        {
            ErrorMessage = AuthHelper.TranslateSignInError(exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = AuthConstants.OfflineMessage;
        }
        catch (Exception exception)
        {
            ErrorMessage = AuthConstants.SignInFailureMessage;
            await HandleExceptionAsync(exception);
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    [RelayCommand]
    public async Task NavigateToRegisterAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.RegisterPage);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }
}
