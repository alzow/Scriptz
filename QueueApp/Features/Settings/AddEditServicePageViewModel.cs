using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Features.Settings.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.ServiceOfferings;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Services.Storage;
using QueueApp.Shared.Templates.QueueEntry.Validators;

namespace QueueApp.Features.Settings;

public partial class AddEditServicePageViewModel : BaseViewModel
{
    private static readonly int[] DurationPresets = { 15, 30, 45, 60, 90 };

    public IValidator NameValidator { get; } = new RequiredValidator("Service name is required.");

    public string Name { get; set; } = "";
    public ObservableCollection<DurationChipOption> DurationChips { get; } = new();
    public bool IsCustomDurationSelected { get; set; }
    public string CustomDurationText { get; set; } = "";
    public string PriceRandText { get; set; } = "";
    public string PriceHelperText { get; } = "Leave this blank and customers will see “Price on request” instead of an amount.";
    public bool IsSaving { get; set; }
    public bool IsDeactivating { get; set; }
    public string PageTitle { get; set; } = "Add Service";
    public bool IsEditMode { get; set; }

    public int EffectiveDurationMinutes =>
        IsCustomDurationSelected
            ? (int.TryParse(CustomDurationText, out var custom) ? custom : 0)
            : DurationChips.FirstOrDefault(c => c.IsSelected && !c.IsCustom)?.Minutes ?? 0;

    public bool IsPriceValid =>
        string.IsNullOrWhiteSpace(PriceRandText) || (decimal.TryParse(PriceRandText, out var rand) && rand >= 0);

    public bool HasNameError =>
        !string.IsNullOrWhiteSpace(Name) &&
        _existingServices.Any(s => s.Id != _editingServiceId && string.Equals(s.Name.Trim(), Name.Trim(), StringComparison.OrdinalIgnoreCase));

    public bool IsSaveEnabled =>
        !string.IsNullOrWhiteSpace(Name) && !HasNameError && EffectiveDurationMinutes > 0 && IsPriceValid && !IsSaving;

    public string PreviewText =>
        $"{(string.IsNullOrWhiteSpace(Name) ? "Service name" : Name.Trim())} · " +
        (EffectiveDurationMinutes > 0 ? $"{EffectiveDurationMinutes} min · " : "") +
        (string.IsNullOrWhiteSpace(PriceRandText)
            ? "Price on request"
            : decimal.TryParse(PriceRandText, out var previewPrice) ? $"R{previewPrice:0.##}" : "R—");

    public bool IsDirty =>
        Name.Trim() != _originalName ||
        EffectiveDurationMinutes != _originalDurationMinutes ||
        PriceRandText.Trim() != _originalPriceText;

    private Guid _businessId;
    private Guid? _editingServiceId;
    private List<ServiceResponse> _existingServices = new();
    private string _businessMode = "queue";
    private string _originalName = "";
    private int _originalDurationMinutes;
    private string _originalPriceText = "";

    private readonly IServiceOfferingsService _serviceOfferingsService;
    private readonly IBusinessService _businessService;
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IQueuePopupService _popupService;

    public AddEditServicePageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IServiceOfferingsService serviceOfferingsService,
        IBusinessService businessService,
        IQueueService queueService,
        IBookingService bookingService,
        IQueuePopupService popupService)
        : base(navigationService, secureStorageService)
    {
        _serviceOfferingsService = serviceOfferingsService;
        _businessService = businessService;
        _queueService = queueService;
        _bookingService = bookingService;
        _popupService = popupService;

        foreach (var minutes in DurationPresets)
            DurationChips.Add(new DurationChipOption(minutes, minutes.ToString()));
        DurationChips.Add(new DurationChipOption(0, "Custom", isCustom: true));
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = parameters is not null && parameters.TryGetValue(NavigationKeys.BusinessId, out var bizObj)
                ? (Guid)bizObj
                : throw new InvalidOperationException("AddEditServicePage requires a businessId.");

            var business = await _businessService.GetBusinessAsync(_businessId);
            _businessMode = business?.Mode ?? "queue";

            _existingServices = await _serviceOfferingsService.GetServicesAsync(_businessId);

            if (parameters is not null && parameters.TryGetValue(NavigationKeys.ServiceId, out var svcObj))
            {
                _editingServiceId = (Guid)svcObj;
                PageTitle = "Edit Service";
                IsEditMode = true;

                var existing = _existingServices.FirstOrDefault(s => s.Id == _editingServiceId);
                if (existing is not null)
                {
                    Name = existing.Name;
                    PriceRandText = existing.PriceCents.HasValue ? (existing.PriceCents.Value / 100m).ToString("0.##") : "";

                    var matchingChip = DurationChips.FirstOrDefault(c => !c.IsCustom && c.Minutes == existing.EstMinutes);
                    if (matchingChip is not null)
                    {
                        matchingChip.IsSelected = true;
                    }
                    else
                    {
                        IsCustomDurationSelected = true;
                        CustomDurationText = existing.EstMinutes.ToString();
                        DurationChips.First(c => c.IsCustom).IsSelected = true;
                    }
                }
            }

            _originalName = Name.Trim();
            _originalDurationMinutes = EffectiveDurationMinutes;
            _originalPriceText = PriceRandText.Trim();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public async Task<(int Count, string WhenText)> CountUpcomingUsesAsync()
    {
        if (_editingServiceId is null)
            return (0, "");

        if (_businessMode == "booking")
        {
            var bookings = await _bookingService.GetBookingsInRangeAsync(
                _businessId, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

            var upcoming = bookings
                .Where(b => b.ServiceId == _editingServiceId &&
                            b.StartsAt > DateTimeOffset.UtcNow &&
                            b.Status != BookingStatuses.Cancelled &&
                            b.Status != BookingStatuses.NoShow)
                .OrderBy(b => b.StartsAt)
                .ToList();

            var whenText = upcoming.Count > 0 ? $"on {upcoming[0].LocalStart:ddd d MMM} at {upcoming[0].LocalStart:HH:mm}" : "";
            return (upcoming.Count, whenText);
        }

        var entries = await _queueService.GetActiveEntriesAsync(_businessId);
        return (entries.Count(e => e.ServiceId == _editingServiceId), "");
    }

    [RelayCommand]
    public void SelectDurationChip(DurationChipOption chip)
    {
        foreach (var candidate in DurationChips)
            candidate.IsSelected = ReferenceEquals(candidate, chip);
        IsCustomDurationSelected = chip.IsCustom;
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        try
        {
            if (IsDirty)
            {
                var discard = await _popupService.ShowConfirmAsync(
                    "Discard changes?", "You haven't saved this service.", "Discard", "Keep editing");
                if (!discard)
                    return;
            }

            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (!IsSaveEnabled)
            return;

        IsSaving = true;
        try
        {
            int? priceCents = null;
            if (!string.IsNullOrWhiteSpace(PriceRandText) && decimal.TryParse(PriceRandText, out var rand))
                priceCents = (int)Math.Round(rand * 100);

            if (_editingServiceId is null)
            {
                await _serviceOfferingsService.CreateServiceAsync(new CreateServiceRequest
                {
                    BusinessId = _businessId,
                    Name = Name.Trim(),
                    EstMinutes = EffectiveDurationMinutes,
                    PriceCents = priceCents,
                });
            }
            else
            {
                await _serviceOfferingsService.UpdateServiceAsync(_editingServiceId.Value, new UpdateServiceRequest
                {
                    Name = Name.Trim(),
                    EstMinutes = EffectiveDurationMinutes,
                    PriceCents = priceCents,
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

    [RelayCommand]
    public async Task DeactivateAsync()
    {
        if (_editingServiceId is null)
            return;

        IsDeactivating = true;
        try
        {
            var (count, whenText) = await CountUpcomingUsesAsync();
            if (count > 0)
            {
                var noun = _businessMode == "booking"
                    ? (count == 1 ? "booking" : "bookings")
                    : (count == 1 ? "customer is" : "customers are");
                var when = string.IsNullOrEmpty(whenText) ? "" : $" — the next one {whenText}";
                var message = _businessMode == "booking"
                    ? $"{count} upcoming {noun} still use this service{when}. They won't be cancelled, but customers won't be able to book it again until you reactivate it."
                    : $"{count} {noun} currently waiting for this service. They'll keep their spot, but it won't be offered to new customers until you reactivate it.";

                var confirmed = await _popupService.ShowConfirmAsync(
                    "Deactivate this service?", message, "Deactivate", "Keep it active");
                if (!confirmed)
                    return;
            }

            await _serviceOfferingsService.SetServiceActiveAsync(_editingServiceId.Value, false);
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsDeactivating = false;
        }
    }
}
