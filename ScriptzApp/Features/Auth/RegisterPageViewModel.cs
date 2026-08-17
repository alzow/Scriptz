using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Constants;
using ScriptzApp.Framework.Base;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;

namespace ScriptzApp.Features.Auth;

public partial class RegisterPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    public RegisterPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        Title = "Register";
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

        // TODO (Step 5): replace with Supabase phone-OTP sign-up. For now this just
        // stores a placeholder session so downstream screens have a token to send.
        await ExecuteAsync(async () =>
        {
            await _authService.SetSessionAsync("placeholder-token", null);
            await _popupService.ShowAlertAsync("Success", "Account created successfully!");
            await NavigationService.NavigateAsync(NavigationPaths.Dashboard);
        });
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
