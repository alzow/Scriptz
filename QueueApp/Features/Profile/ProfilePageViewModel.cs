using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Features.Profile.Helpers;
using QueueApp.Shared.Domain;
using QueueApp.Constants;
using QueueApp.Features.Profile.Sheets;
using QueueApp.Framework.Base;
using QueueApp.Framework.Theming;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Profile;
using QueueApp.Services.Auth;
using QueueApp.Services.Notifications;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Profile;

public partial class ProfilePageViewModel : BaseViewModel
{
    public const string AllowedHeadline = "You'll be told when to leave";
    public const string BlockedHeadline = "Notifications are off";
    public const string BlockedDetail = "You won't be told when it's your turn";
    public const string BlockedAction = "Turn on notifications";
    public const string NotificationsOffRowDetail = "Off — nothing will reach you";

    public string DisplayName { get; set; } = "";
    public string Initials { get; set; } = "";
    public string Email { get; set; } = "";
    public bool HasEmail => !string.IsNullOrWhiteSpace(Email);

    public bool NotificationsAllowed { get; set; } = true;
    public string StatusHeadline { get; set; } = AllowedHeadline;
    public string StatusDetail { get; set; } = "";
    public string StatusActionText { get; set; } = "";

    public string DetailsRowDetail { get; set; } = "";
    public string NotificationsRowDetail { get; set; } = "";
    public string AppearanceRowDetail { get; set; } = "";

    public bool OwnsBusiness { get; set; }
    public string BusinessName { get; set; } = "";
    public string BusinessDetail { get; set; } = "";

    public string VersionText { get; set; } = "";

    private Guid _userId;
    private string _phone = "";

    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly IBusinessService _businessService;
    private readonly INotificationPermissionService _notificationPermissionService;
    private readonly INotificationPreferencesService _notificationPreferencesService;
    private readonly IQueuePopupService _popupService;

    public ProfilePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IProfileService profileService,
        IBusinessService businessService,
        INotificationPermissionService notificationPermissionService,
        INotificationPreferencesService notificationPreferencesService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _profileService = profileService;
        _businessService = businessService;
        _notificationPermissionService = notificationPermissionService;
        _notificationPreferencesService = notificationPreferencesService;
        _popupService = popupService;

        Title = "Profile";
        VersionText = $"Queue {AppInfo.Current.VersionString} · build {AppInfo.Current.BuildString}";
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            await LoadBusinessAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            await RefreshPermissionAsync();
            await LoadIdentityAsync();
            RefreshAppearanceRow();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    public async Task LoadIdentityAsync()
    {
        try
        {
            var userIdRaw = await _authService.GetUserIdAsync();
            if (!Guid.TryParse(userIdRaw, out var userId))
                return;

            _userId = userId;

            var profile = await _profileService.GetMyProfileAsync(userId);
            DisplayName = string.IsNullOrWhiteSpace(profile?.DisplayName) ? "Customer" : profile!.DisplayName!;
            Initials = TextFormat.Initials(DisplayName);

            _phone = profile?.Phone ?? "";
            DetailsRowDetail = string.IsNullOrWhiteSpace(_phone) ? DisplayName : $"{DisplayName} · {_phone}";

            Email = await _authService.GetUserEmailAsync() ?? "";
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task LoadBusinessAsync()
    {
        try
        {
            var businessId = await _businessService.GetOwnedBusinessIdAsync();
            if (businessId == Guid.Empty)
            {
                OwnsBusiness = false;
                return;
            }

            var business = await _businessService.GetBusinessAsync(businessId);
            if (business is null)
            {
                OwnsBusiness = false;
                return;
            }

            OwnsBusiness = true;
            BusinessName = business.Name;
            BusinessDetail = string.IsNullOrWhiteSpace(business.Suburb)
                ? ProfileHelper.CategoryLabel(business.Category)
                : $"{ProfileHelper.CategoryLabel(business.Category)} · {business.Suburb}";
        }
        catch (Exception)
        {
            OwnsBusiness = false;
        }
    }

    public async Task RefreshPermissionAsync()
    {
        try
        {
            NotificationsAllowed = await _notificationPermissionService.IsAllowedAsync();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public void RefreshStatus()
    {
        try
        {
            var preferences = _notificationPreferencesService.Get();

            if (NotificationsAllowed)
            {
                StatusHeadline = AllowedHeadline;
                StatusDetail = $"Notifications on · nudge {preferences.LeaveAtMinutes} min before";
                StatusActionText = "";
                NotificationsRowDetail = $"{preferences.OnCount} of {NotificationPreferences.TotalCount} on";
                return;
            }

            StatusHeadline = BlockedHeadline;
            StatusDetail = BlockedDetail;
            StatusActionText = BlockedAction;
            NotificationsRowDetail = NotificationsOffRowDetail;
        }
        catch (Exception exception)
        {
            _ = HandleExceptionAsync(exception);
        }
    }

    public void RefreshAppearanceRow() =>
        AppearanceRowDetail = ThemeService.Current switch
        {
            ThemeChoice.Light => "Always light",
            ThemeChoice.Dark => "Always dark",
            _ => "Follow system",
        };

    [RelayCommand]
    public async Task OpenDetailsAsync()
    {
        try
        {
            if (_userId == Guid.Empty)
                await LoadIdentityAsync();

            if (_userId == Guid.Empty)
                return;

            var sheet = new YourDetailsSheet(_profileService, _popupService, _userId, DisplayName, _phone, Email);
            await _popupService.ShowSheetAsync(sheet);

            if (await sheet.Completion)
                await LoadIdentityAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenNotificationsAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.ProfileNotificationsPage}", modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenAppearanceAsync()
    {
        try
        {
            var sheet = new AppearanceSheet(_popupService);
            await _popupService.ShowSheetAsync(sheet);
            await sheet.Completion;
            RefreshAppearanceRow();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenAccountAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.ProfileAccountPage}", modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenBusinessAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(
                $"NavigationPage/{NavigationPaths.BusinessSettingsPage}", modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task TurnOnNotificationsAsync()
    {
        try
        {
            var allowed = await _notificationPermissionService.RequestAsync();
            if (!allowed)
                _notificationPermissionService.OpenAppSettings();

            await RefreshPermissionAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SignOutAsync()
    {
        try
        {
            var confirmed = await _popupService.ShowConfirmAsync(
                "Sign out?", "You'll need your email and password to sign back in.", "Sign out", "Stay signed in");
            if (!confirmed)
                return;

            await _authService.SignOutAsync();
            _profileService.InvalidateCache();
            await NavigationService.NavigateAsync(NavigationPaths.Login);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
