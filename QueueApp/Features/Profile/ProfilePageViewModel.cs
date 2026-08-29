using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Framework.Theming;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Profile;

public class ProfilePageViewModel : BaseViewModel
{
    public ProfilePageViewModel(INavigationService navigationService, ISecureStorageService secureStorageService)
        : base(navigationService, secureStorageService)
    {
        SetThemeCommand = new RelayCommand<string>(SetTheme);
    }

    public IRelayCommand<string> SetThemeCommand { get; }

    // Three bools rather than one enum: the chips bind their selected state straight off these,
    // the same shape the history filter bar uses.
    private bool _isSystemTheme = true;
    public bool IsSystemTheme
    {
        get => _isSystemTheme;
        private set => SetProperty(ref _isSystemTheme, value);
    }

    private bool _isLightTheme;
    public bool IsLightTheme
    {
        get => _isLightTheme;
        private set => SetProperty(ref _isLightTheme, value);
    }

    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        private set => SetProperty(ref _isDarkTheme, value);
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            SyncThemeSelection();
            // TODO: profile info (name, phone once phone-OTP lands), T&Cs link,
            // and the "Become an operator" entry point — registering a business,
            // which per 4d's note will need a re-navigation into MainTabbedPage
            // to pick up the new Manage tab once that flow exists.
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private void SetTheme(string? choice)
    {
        if (!Enum.TryParse<ThemeChoice>(choice, out var parsed))
            return;

        ThemeService.Set(parsed);
        SyncThemeSelection();
    }

    private void SyncThemeSelection()
    {
        IsSystemTheme = ThemeService.Current == ThemeChoice.System;
        IsLightTheme = ThemeService.Current == ThemeChoice.Light;
        IsDarkTheme = ThemeService.Current == ThemeChoice.Dark;
    }
}
