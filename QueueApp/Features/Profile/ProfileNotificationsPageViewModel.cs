using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Notifications;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Profile;

public partial class ProfileNotificationsPageViewModel : BaseViewModel
{
    public const string AllowedHeadline = "Allowed on this phone";
    public const string AllowedDetail = "Turn these off in your phone settings at any time";
    public const string BlockedHeadline = "Blocked by your phone";
    public const string BlockedDetail = "Queue can't send you anything until you allow it";
    public const string BlockedAction = "Open phone settings";

    public bool IsAllowed { get; set; } = true;
    public bool IsBlocked => !IsAllowed;

    public string StatusHeadline => IsAllowed ? AllowedHeadline : BlockedHeadline;
    public string StatusDetail => IsAllowed ? AllowedDetail : BlockedDetail;
    public string StatusActionText => IsAllowed ? "" : BlockedAction;

    public bool TimeToLeave { get; set; } = true;
    public bool YoureNext { get; set; } = true;
    public bool QueueChanged { get; set; } = true;
    public bool BookingConfirmed { get; set; } = true;
    public bool BookingReminders { get; set; } = true;
    public bool AwaitingCollectionReady { get; set; } = true;
    public int LeaveAtMinutes { get; set; } = 10;

    private readonly INotificationPermissionService _permissionService;
    private readonly INotificationPreferencesService _preferencesService;

    public ProfileNotificationsPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        INotificationPermissionService permissionService,
        INotificationPreferencesService preferencesService)
        : base(navigationService, secureStorageService)
    {
        _permissionService = permissionService;
        _preferencesService = preferencesService;

        Title = "Notifications";
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            LoadPreferences();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        IsAllowed = await _permissionService.IsAllowedAsync();
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        SavePreferences();
    }

    public void LoadPreferences()
    {
        var preferences = _preferencesService.Get();
        TimeToLeave = preferences.TimeToLeave;
        YoureNext = preferences.YoureNext;
        QueueChanged = preferences.QueueChanged;
        BookingConfirmed = preferences.BookingConfirmed;
        BookingReminders = preferences.BookingReminders;
        AwaitingCollectionReady = preferences.AwaitingCollectionReady;
        LeaveAtMinutes = preferences.LeaveAtMinutes;
    }

    public void SavePreferences() =>
        _preferencesService.Save(new NotificationPreferences
        {
            TimeToLeave = TimeToLeave,
            YoureNext = YoureNext,
            QueueChanged = QueueChanged,
            BookingConfirmed = BookingConfirmed,
            BookingReminders = BookingReminders,
            AwaitingCollectionReady = AwaitingCollectionReady,
            LeaveAtMinutes = LeaveAtMinutes,
        });

    [RelayCommand]
    public void SetLeaveAt(int minutes)
    {
        LeaveAtMinutes = minutes;
        SavePreferences();
    }

    [RelayCommand]
    public async Task OpenPhoneSettingsAsync()
    {
        try
        {
            SavePreferences();
            _permissionService.OpenAppSettings();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            SavePreferences();
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
