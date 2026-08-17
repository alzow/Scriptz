using CommunityToolkit.Mvvm.Input;
using ScriptzApp.Constants;
using ScriptzApp.Framework.Base;
using ScriptzApp.Models.Api.Requests;
using ScriptzApp.Services.Auth;
using ScriptzApp.Services.Storage;
using ScriptzApp.Services.Popup;

namespace ScriptzApp.Features.Auth;

public partial class LoginPageViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IScriptzPopupService _popupService;

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public LoginPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IScriptzPopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _popupService = popupService;
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await _popupService.ShowAlertAsync("Validation Error", "Please enter email and password");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var request = new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password
            };

            var result = await _authService.LoginAsync(request);

            if (result != null)
            {
                await NavigationService.NavigateAsync(NavigationPaths.Dashboard);
            }
            else
            {
                await _popupService.ShowAlertAsync("Login Failed", "Invalid email or password");
            }
        });
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.RegisterPage);
    }
}
