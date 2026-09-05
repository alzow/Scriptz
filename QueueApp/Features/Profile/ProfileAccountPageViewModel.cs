using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
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

    private readonly IQueuePopupService _popupService;

    public ProfileAccountPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _popupService = popupService;

        Title = "Account and privacy";
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
    public async Task GoBackAsync()
    {
        try
        {
            await RunNavigationAsync(() => NavigationService.GoBackAsync());
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
