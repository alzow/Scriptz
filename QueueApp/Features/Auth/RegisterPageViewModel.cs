using CommunityToolkit.Mvvm.Input;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;
using QueueApp.Services.Popup;
using System.Diagnostics;

namespace QueueApp.Features.Auth;

public partial class RegisterPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IQueuePopupService _popupService;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsSigningUp { get; set; }

    public RegisterPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        Debug.WriteLine("REGISTER LOADED");
        await base.OnLoadedAsync(parameters);
    }


    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please fill in all required fields");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await _popupService.ShowAlertAsync("Validation Error", "Passwords do not match");
            return;
        }

        if (Password.Length < 6)
        {
            await _popupService.ShowAlertAsync("Validation Error", "Password must be at least 6 characters");
            return;
        }

        // TODO (Step 5b): swap for Supabase phone-OTP sign-up. Token pipeline stays the same.

        IsSigningUp = true;
        try
        {
            var ok = await _authService.SignUpAsync(Email.Trim(), Password);

            if (ok)
            {
                await _popupService.ShowAlertAsync("Success", "Account created successfully!");
                await NavigationService.NavigateAsync($"/{NavigationPaths.OperatorQueuePage}");
            }
            else
            {
                await _popupService.ShowAlertAsync("Registration Failed", "Unable to create account. Please try again.");
            }
        }
        finally
        {
            IsSigningUp = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
