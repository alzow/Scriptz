using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Location;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BusinessSettings.BusinessLocation;

// Captures the business's map location via device GPS — the owner is presumably standing in
// their shop when they do this, so there's no manual pin-drop/address entry, just "use my
// current location". Writes to the already-existing (but previously unused) businesses.latitude
// / longitude columns, which is what lets the customer-facing Browse dashboard compute distance.
public partial class BusinessLocationPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly ILocationService _locationService;
    private readonly IQueuePopupService _popupService;
    private Guid _businessId;

    public BusinessLocationPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        ILocationService locationService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _locationService = locationService;
        _popupService = popupService;
        Title = "Location";
    }

    public bool IsLoading { get; set; }
    public bool IsCapturing { get; set; }
    public bool HasLocation { get; set; }
    public string CoordinatesText { get; set; } = string.Empty;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            IsLoading = true;

            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            var business = await _businessService.GetBusinessAsync(_businessId);
            ApplyBusiness(business?.Latitude, business?.Longitude);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyBusiness(double? latitude, double? longitude)
    {
        HasLocation = latitude.HasValue && longitude.HasValue;
        CoordinatesText = HasLocation ? $"{latitude:0.00000}, {longitude:0.00000}" : string.Empty;
    }

    [RelayCommand]
    public async Task CaptureLocationAsync()
    {
        IsCapturing = true;
        try
        {
            var result = await _locationService.RefreshLocationAsync();
            if (result.Location is null)
            {
                await _popupService.ShowAlertAsync("Couldn't get your location",
                    "Check that location permission is granted for this app and that location services are on, then try again.");
                return;
            }

            var location = result.Location;
            await _businessService.UpdateLocationAsync(_businessId, location.Latitude, location.Longitude);
            ApplyBusiness(location.Latitude, location.Longitude);
            await _popupService.ShowAlertAsync("Location saved",
                "Customers browsing nearby will now see how far they are from you.");
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsCapturing = false;
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
