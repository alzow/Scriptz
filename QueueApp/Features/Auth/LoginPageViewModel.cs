using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
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
    #region Constants
    private const string InvalidCredentialsMessage = "That email and password don't match an account.";
    private const string EmailNotConfirmedMessage = "Confirm your email address first — check your inbox for the link.";
    private const string RateLimitedMessage = "Too many attempts. Wait a minute and try again.";
    private const string OfflineMessage = "No connection. Check your internet and try again.";
    private const string GenericFailureMessage = "Couldn't sign you in. Please try again.";
    #endregion

    #region Properties
    public ISharedStateManager FormStateManager { get; } = new FormValidators.SharedStateManager();

    public IValidator EmailValidator { get; } = new FormValidators.EmailValidator("Enter a valid email address.");
    public IValidator PasswordValidator { get; } = new FormValidators.RequiredValidator("Enter your password.");

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsFormValid { get; set; }
    public bool IsSigningIn { get; set; }

    public bool CanSubmit => IsFormValid;
    #endregion

    #region Services
    private readonly IAuthService _authService;
    private readonly IBusinessService _businessService;
    #endregion

    #region Constructor
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
    #endregion

    public void OnFormValidationStateChanged(bool isValid) => IsFormValid = isValid;

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
            ErrorMessage = TranslateSignInError(exception);
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

    public static string TranslateSignInError(ApiException exception)
    {
        var body = exception.Content ?? string.Empty;

        if (body.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase)
            || body.Contains("email_not_confirmed", StringComparison.OrdinalIgnoreCase))
            return EmailNotConfirmedMessage;

        if (body.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase)
            || body.Contains("invalid_credentials", StringComparison.OrdinalIgnoreCase)
            || exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
            return InvalidCredentialsMessage;

        if (body.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
            || exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            return RateLimitedMessage;

        return GenericFailureMessage;
    }
}
