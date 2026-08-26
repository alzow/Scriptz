using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Framework.Extensions;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BookingAgenda.Sheets;

public sealed class AddBookingServiceOption : ObservableObject
{
    public required ServiceResponse Service { get; init; }
    public string Name => Service.Name;

    // Dimmed and labelled rather than hidden: a service that vanishes from the list makes the
    // operator wonder whether it still exists at all (spec §7.1).
    public required bool Fits { get; init; }
    public string MetaText { get; init; } = "";
    public double Opacity => Fits ? 1 : 0.38;
    public bool IsSelected { get; set; }
}

// The phone still rings. A booking business takes more bookings by phone than through the app for a
// long while, and if the operator can't enter those, the agenda is fiction (spec §7).
public partial class AddBookingSheetViewModel : ObservableObject
{
    private readonly Guid _businessId;
    private readonly DateTimeOffset _windowStart;
    private readonly DateTimeOffset _windowEnd;

    public AddBookingSheetViewModel(
        Guid businessId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        IReadOnlyList<ServiceResponse> services,
        IReadOnlyList<OperatorResponse> operators,
        CategoryLabelSet labels)
    {
        _businessId = businessId;
        _windowStart = windowStart;
        _windowEnd = windowEnd;

        WindowText = $"{windowStart:ddd d} · {windowStart:HH:mm} – {windowEnd:HH:mm} free";

        var windowMinutes = (int)(windowEnd - windowStart).TotalMinutes;

        foreach (var service in services)
        {
            var fits = service.EstMinutes <= windowMinutes;
            Services.Add(new AddBookingServiceOption
            {
                Service = service,
                Fits = fits,
                MetaText = fits
                    ? $"{service.EstMinutes} min · {MoneyFormatOrDash(service)}"
                    : $"{service.EstMinutes} min · won't fit",
            });
        }

        foreach (var op in operators)
            Resources.Add(new BayFilterOption { OperatorId = op.Id, Label = op.DisplayName });

        var firstFitting = Services.FirstOrDefault(s => s.Fits);
        if (firstFitting is not null)
            Select(firstFitting);

        var firstResource = Resources.FirstOrDefault();
        if (firstResource is not null)
            firstResource.IsSelected = true;

        ResourceLabel = labels.SectionTitle.ToUpperInvariant();
    }

    public string WindowText { get; }
    public string ResourceLabel { get; }

    public ObservableCollection<AddBookingServiceOption> Services { get; } = new();
    public ObservableCollection<BayFilterOption> Resources { get; } = new();

    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";

    public string SlotText { get; set; } = "";

    // The shop created it, so there's nobody to confirm with — and the sheet says so rather than
    // leaving the operator to wonder whether the customer still has to accept something.
    public string StatusText => "CONFIRMED";

    public bool IsSaving { get; set; }

    public bool CanSubmit =>
        !IsSaving &&
        !string.IsNullOrWhiteSpace(CustomerName) &&
        Services.Any(s => s.IsSelected && s.Fits) &&
        Resources.Any(r => r.IsSelected);

    public Func<CreateOperatorBookingRequest, Task>? OnCreate { get; init; }
    public Func<Task>? OnDismiss { get; init; }

    [RelayCommand]
    private void SelectService(AddBookingServiceOption option)
    {
        if (!option.Fits)
            return;

        Select(option);
        OnPropertyChanged(nameof(CanSubmit));
    }

    [RelayCommand]
    private void SelectResource(BayFilterOption option)
    {
        foreach (var resource in Resources)
            resource.IsSelected = resource == option;

        OnPropertyChanged(nameof(CanSubmit));
    }

    [RelayCommand]
    private Task DismissAsync() => OnDismiss?.Invoke() ?? Task.CompletedTask;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit)
            return;

        var service = Services.First(s => s.IsSelected && s.Fits).Service;
        var resource = Resources.First(r => r.IsSelected);

        IsSaving = true;
        try
        {
            var request = new CreateOperatorBookingRequest
            {
                BusinessId = _businessId,
                OperatorId = resource.OperatorId!.Value,
                ServiceId = service.Id,
                StartsAt = _windowStart,
                EndsAt = _windowStart.AddMinutes(service.EstMinutes),
                Status = BookingStatuses.Confirmed,
                Details = new BookingDetails
                {
                    CustomerName = CustomerName.Trim(),
                    CustomerPhone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    CreatedBy = "operator",
                },
            };

            if (OnCreate is not null)
                await OnCreate(request);
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Select(AddBookingServiceOption option)
    {
        foreach (var service in Services)
            service.IsSelected = service == option;

        // The slot and the end time it produces are shown before anything is committed.
        var end = _windowStart.AddMinutes(option.Service.EstMinutes);
        SlotText = $"{_windowStart:HH:mm} – {end:HH:mm}";
    }

    private static string MoneyFormatOrDash(ServiceResponse service) =>
        service.PriceCents is null ? "no price" : MoneyFormat.Format(service.PriceCents);
}
