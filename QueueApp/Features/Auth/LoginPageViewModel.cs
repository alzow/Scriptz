using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Framework.Navigation;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.AlzowEntry.Validators;

namespace QueueApp.Features.Auth;

public partial class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;
    private readonly IBusinessService _businessService;

    public string Email { get; set; } = "alzow.sayed01@gmail.com";
    public string Password { get; set; } = "S@yed786";
    public bool IsSigningIn { get; set; }
    public bool IsFormValid { get; set; }

    public ISharedStateManager FormStateManager { get; } = new SharedStateManager();
    public IValidator EmailValidator { get; } = new RequiredValidator("Enter your email.");
    public IValidator PasswordValidator { get; } = new RequiredValidator("Enter your password.");

    public LoginPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueuePopupService popupService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        _businessService = businessService;
        FormStateManager.ValidationStateChanged += isValid => IsFormValid = isValid;
    }

    // TODO (Step 5b): swap for Supabase phone-OTP sign-in. Token pipeline stays the same.
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please enter email and password");
            return;
        }

        IsSigningIn = true;
        try
        {
            var ok = await _authService.SignInAsync(Email.Trim(), Password);

            if (ok)
            {
                var (ownsBusiness, mode) = await MainTabbedNavigation.TryGetOwnedBusinessAsync(_businessService);
                var uri = MainTabbedNavigation.BuildMainTabbedUri(includeManageTab: ownsBusiness, manageMode: mode);
                await NavigationService.NavigateAsync(uri);
            }
            else
            {
                await _popupService.ShowAlertAsync("Login Failed", "Invalid email or password");
            }
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.RegisterPage);
    }
}
