using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.Profile.Models;
using QueueApp.Features.Profile.Sheets;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Auth;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Profile;

public partial class ProfileAccountPageViewModel : BaseViewModel
{
    public const string ShopsCanSeeTitle = "What shops can see";
    public const string ShopsCanSeeBody =
        "A shop you join sees the name on your profile, your phone number, and the visits you've " +
        "made to that shop. It can't see your email, or anything you've done at another shop.";

    public bool HasPrivacyPolicy => SupportLinks.HasPrivacyPolicy;
    public bool HasTermsOfUse => SupportLinks.HasTermsOfUse;
    public bool HasDataRequest => SupportLinks.HasDataRequestEmail;
    public bool HasLegalSection => HasPrivacyPolicy || HasTermsOfUse;
    public string TermsDetail => SupportLinks.TermsLastUpdated;

    private readonly IAuthService _authService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IQueuePopupService _popupService;

    public ProfileAccountPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IAuthService authService,
        IQueueService queueService,
        IBookingService bookingService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _authService = authService;
        _queueService = queueService;
        _bookingService = bookingService;
        _popupService = popupService;

        Title = "Account and privacy";
    }

    public async Task<List<DeleteConsequenceItem>> LoadConsequencesAsync()
    {
        var items = new List<DeleteConsequenceItem>();

        var activeEntry = await _queueService.GetMyActiveEntryAsync();
        if (activeEntry is not null)
        {
            items.Add(new DeleteConsequenceItem
            {
                Title = activeEntry.BusinessName,
                Detail = activeEntry.IsBeingServed
                    ? "Being served now"
                    : $"In the queue · position {activeEntry.Position}",
            });
        }

        var userIdRaw = await _authService.GetUserIdAsync();
        if (Guid.TryParse(userIdRaw, out var userId))
        {
            var bookings = await _bookingService.GetMyUpcomingBookingsAsync(userId);
            items.AddRange(bookings
                .Where(booking => booking.EndsAt >= DateTimeOffset.UtcNow)
                .Select(booking => new DeleteConsequenceItem
                {
                    Title = booking.BusinessName,
                    Detail = booking.DateTimeDisplay,
                }));
        }

        return items;
    }

    [RelayCommand]
    public async Task DownloadMyDataAsync()
    {
        try
        {
            await Email.Default.ComposeAsync(new EmailMessage(
                "Please send me my Queue data",
                "I'd like a copy of everything Queue holds about my account.",
                SupportLinks.DataRequestEmail));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task ShowWhatShopsSeeAsync()
    {
        try
        {
            await _popupService.ShowAlertAsync(ShopsCanSeeTitle, ShopsCanSeeBody);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenPrivacyPolicyAsync()
    {
        try
        {
            await Browser.Default.OpenAsync(SupportLinks.PrivacyPolicyUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenTermsAsync()
    {
        try
        {
            await Browser.Default.OpenAsync(SupportLinks.TermsOfUseUrl, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task DeleteAccountAsync()
    {
        try
        {
            var consequences = await LoadConsequencesAsync();

            var sheet = new DeleteAccountSheet(_authService, _popupService, consequences);
            await _popupService.ShowSheetAsync(sheet);

            if (await sheet.Completion)
                await NavigationService.NavigateAsync(NavigationPaths.Login);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            await _popupService.ShowAlertAsync(
                "Couldn't check your bookings",
                "We couldn't load what deleting your account would cancel, so we haven't started. Try again in a moment.");
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
