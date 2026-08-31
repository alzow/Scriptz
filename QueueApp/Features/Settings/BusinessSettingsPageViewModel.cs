using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.CategoryPicker.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Settings;

public partial class BusinessSettingsPageViewModel : BaseViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public bool IsBookingMode { get; set; }

    public string ServicesValueText { get; set; } = string.Empty;
    public bool ServicesValueIsMissing { get; set; }
    public string StaffValueText { get; set; } = string.Empty;
    public bool StaffValueIsMissing { get; set; }
    public string HoursValueText { get; set; } = string.Empty;
    public bool HoursValueIsMissing { get; set; }
    public string LocationValueText { get; set; } = string.Empty;
    public bool LocationValueIsMissing { get; set; }

    public bool IsLoading { get; set; } = true;

    private Guid _businessId;

    private readonly IBusinessService _businessService;
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IOperatorService _operatorService;

    public BusinessSettingsPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IServiceOfferingsService serviceOfferingsService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _serviceOfferingsService = serviceOfferingsService;
        _operatorService = operatorService;
        Title = "Business settings";
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
        if (_businessId != Guid.Empty)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _businessId = await _businessService.GetOwnedBusinessIdAsync();

            var businessTask = _businessService.GetBusinessAsync(_businessId);
            var servicesTask = _serviceOfferingsService.GetServicesAsync(_businessId);
            var operatorsTask = _operatorService.GetAllOperatorsForManagementAsync(_businessId);

            await Task.WhenAll(businessTask, servicesTask, operatorsTask);

            var business = await businessTask;
            var services = await servicesTask;
            var operators = await operatorsTask;

            BusinessName = business?.Name ?? string.Empty;
            IsBookingMode = business?.Mode == "booking";
            CategoryDisplay = CategoryCatalog.All
                .FirstOrDefault(c => c.Key == NormalizeCategory(business?.Category))?.Display ?? "Business";

            var activeServices = services.Count(s => s.IsActive);
            ServicesValueIsMissing = activeServices == 0;
            ServicesValueText = activeServices == 0 ? "No services yet" : $"{activeServices} active";

            var onTeam = operators.Count(o => o.IsActive);
            StaffValueIsMissing = onTeam == 0;
            StaffValueText = onTeam == 0 ? "No staff yet" : $"{onTeam} on the team";

            var activeOperatorIds = operators.Where(o => o.IsActive).Select(o => o.Id).ToList();
            var hours = activeOperatorIds.Count == 0
                ? BusinessHours.Unknown
                : BusinessHours.FromAvailability(await _operatorService.GetAvailabilityAsync(activeOperatorIds));
            HoursValueIsMissing = !hours.HasData;
            HoursValueText = hours.HasData ? hours.SummaryText : "Not set yet";

            var hasAddress = !string.IsNullOrWhiteSpace(business?.Address);
            var hasCoordinates = business?.Latitude.HasValue == true && business?.Longitude.HasValue == true;
            LocationValueIsMissing = !hasAddress && !hasCoordinates;
            LocationValueText = hasAddress
                ? $"{business!.Address}, {business.Suburb}"
                : hasCoordinates
                    ? $"{business!.Latitude:0.00000}, {business.Longitude:0.00000}"
                    : "Not set yet";
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

    public static string NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? string.Empty : category.Replace("_", "").Replace(" ", "").ToLowerInvariant();

    [RelayCommand]
    public async Task OpenServicesAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.ServicesManagementPage);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenStaffAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.StaffManagementPage);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenHoursAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.OperatorHoursPage);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task OpenLocationAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.BusinessLocationPage);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task CloseAsync()
    {
        try
        {
            await NavigationService.GoBackAsync(modal: true, animated: false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
}
