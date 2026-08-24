using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.AlzowEntry.Validators;

namespace QueueApp.Features.Settings;

public partial class AddEditServicePageViewModel : BaseViewModel
{
    private readonly IServiceOfferingsService _serviceOfferingsService;
    private Guid _businessId;
    private Guid? _editingServiceId;

    public AddEditServicePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        FormStateManager.ValidationStateChanged += isValid => IsFormValid = isValid;
    }

    public ISharedStateManager FormStateManager { get; } = new SharedStateManager();
    public IValidator NameValidator { get; } = new RequiredValidator("Service name is required.");
    public IValidator DurationValidator { get; } = new RequiredValidator("Duration is required.");

    public string Name { get; set; } = "";
    public string DurationMinutesText { get; set; } = "";
    public string PriceRandText { get; set; } = "";
    public bool IsFormValid { get; set; }
    public bool IsSaving { get; set; }
    public string PageTitle { get; set; } = "Add Service";

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditServicePage requires a businessId.");

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var svcObj))
            {
                _editingServiceId = (Guid)svcObj;
                PageTitle = "Edit Service";
                await LoadExistingAsync(_editingServiceId.Value);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private async Task LoadExistingAsync(Guid serviceId)
    {
        var services = await _serviceOfferingsService.GetServicesAsync(_businessId);
        var existing = services.FirstOrDefault(s => s.Id == serviceId);
        if (existing is null) return;

        Name = existing.Name;
        DurationMinutesText = existing.EstMinutes.ToString();
        PriceRandText = existing.PriceCents.HasValue ? (existing.PriceCents.Value / 100m).ToString("0.##") : "";
    }

    [RelayCommand]
    private async Task GoBackAsync()
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            if (!int.TryParse(DurationMinutesText, out var duration) || duration <= 0)
                throw new InvalidOperationException("Duration must be a whole number of minutes.");

            int? priceCents = null;
            if (!string.IsNullOrWhiteSpace(PriceRandText) && decimal.TryParse(PriceRandText, out var rand))
                priceCents = (int)Math.Round(rand * 100);

            if (_editingServiceId is null)
            {
                await _serviceOfferingsService.CreateServiceAsync(new CreateServiceRequest
                {
                    BusinessId = _businessId,
                    Name = Name,
                    EstMinutes = duration,
                    PriceCents = priceCents
                });
            }
            else
            {
                await _serviceOfferingsService.UpdateServiceAsync(_editingServiceId.Value, new UpdateServiceRequest
                {
                    Name = Name,
                    EstMinutes = duration,
                    PriceCents = priceCents
                });
            }

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
