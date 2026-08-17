using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Constants;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Requests;
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

        await ExecuteAsync(async () =>
        {
            var request = new RegisterRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = Email.Trim(),
                Password = Password,
                ConfirmPassword = ConfirmPassword
            };

            var result = await _authService.RegisterAsync(request);

            if (result != null)
            {
                await _popupService.ShowAlertAsync("Success", "Account created successfully!");
                await NavigationService.NavigateAsync(NavigationPaths.Dashboard);
            }
            else
            {
                await _popupService.ShowAlertAsync("Registration Failed", "Unable to create account. Please try again.");
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await NavigationService.GoBackAsync();
    }
}
