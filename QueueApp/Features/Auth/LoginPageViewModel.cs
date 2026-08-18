using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;

namespace QueueApp.Features.Auth;

public partial class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;

    public string Email { get; set; } = "alzow.sayed01@gmail.com";
    public string Password { get; set; } = "S@yed786";
    public bool IsSigningIn { get; set; }

    public LoginPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
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
                await NavigationService.NavigateAsync(NavigationPaths.OperatorQueuePage);
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
