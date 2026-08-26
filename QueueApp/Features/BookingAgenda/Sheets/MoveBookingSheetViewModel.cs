using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Features.BusinessDetail.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;

namespace QueueApp.Features.BookingAgenda.Sheets;

// Rescheduling asks the same question the customer's own booking flow asks — "when are you free?" —
// so it asks the same engine, get_available_slots(_any), and renders the answer with the same
// SlotChoiceItem the customer-side picker uses.
//
// The customer-side picker's *view* isn't extractable today: it's laid out inline inside
// BusinessDetailPage.xaml rather than sitting in a control like ServicePickerView does. Reusing the
// engine and the item model is as far as reuse goes without lifting that markup out first.
public partial class MoveBookingSheetViewModel : ObservableObject
{
    private readonly AgendaBookingResponse _booking;
    private readonly ServiceResponse _service;
    private readonly IBookingService _bookingService;
    private readonly Guid _businessId;

    public MoveBookingSheetViewModel(
        AgendaBookingResponse booking,
        ServiceResponse service,
        IReadOnlyList<OperatorResponse> operators,
        DateTime day,
        IBookingService bookingService,
        Guid businessId)
    {
        _booking = booking;
        _service = service;
        _bookingService = bookingService;
        _businessId = businessId;

        Date = day;
        CustomerName = booking.CustomerName;
        CurrentText = $"Now {booking.DayAndRangeDisplay}";
        ServiceText = $"{service.Name} · {service.EstMinutes} min";

        foreach (var op in operators)
        {
            Resources.Add(new BayFilterOption
            {
                OperatorId = op.Id,
                Label = op.DisplayName,
                IsSelected = op.Id == booking.OperatorId,
            });
        }

        if (!Resources.Any(r => r.IsSelected) && Resources.Count > 0)
            Resources[0].IsSelected = true;
    }

    public string CustomerName { get; }
    public string CurrentText { get; }
    public string ServiceText { get; }

    public DateTime Date { get; set; }
    public DateTime MinimumDate { get; } = LocalTime.Now.Date;

    public ObservableCollection<BayFilterOption> Resources { get; } = new();
    public ObservableCollection<SlotChoiceItem> Slots { get; } = new();

    public bool IsLoadingSlots { get; set; }
    public bool HasSlots => Slots.Count > 0;
    public bool IsEmpty => !IsLoadingSlots && Slots.Count == 0;
    public bool IsSaving { get; set; }
    public bool CanSubmit => !IsSaving && Slots.Any(s => s.IsSelected);

    public Func<AgendaBookingResponse, Guid, DateTimeOffset, DateTimeOffset, Task>? OnMove { get; init; }
    public Func<Task>? OnDismiss { get; init; }

    public async Task LoadAsync()
    {
        IsLoadingSlots = true;
        try
        {
            var resource = Resources.FirstOrDefault(r => r.IsSelected);

            var slots = resource?.OperatorId is Guid operatorId
                ? await _bookingService.GetAvailableSlotsAsync(operatorId, _service.Id, Date)
                : await _bookingService.GetAvailableSlotsAnyAsync(_businessId, _service.Id, Date);

            Slots.Clear();
            foreach (var slot in slots)
            {
                Slots.Add(new SlotChoiceItem
                {
                    Slot = slot,
                    TimeText = slot.SlotStart.ToOffset(LocalTime.Offset).ToString("HH:mm"),
                });
            }
        }
        finally
        {
            IsLoadingSlots = false;
            OnPropertyChanged(nameof(HasSlots));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(CanSubmit));
        }
    }

    [RelayCommand]
    private async Task SelectResourceAsync(BayFilterOption option)
    {
        foreach (var resource in Resources)
            resource.IsSelected = resource == option;

        await LoadAsync();
    }

    [RelayCommand]
    private void SelectSlot(SlotChoiceItem item)
    {
        foreach (var slot in Slots)
            slot.IsSelected = slot == item;

        OnPropertyChanged(nameof(CanSubmit));
    }

    [RelayCommand]
    private Task DateChangedAsync() => LoadAsync();

    [RelayCommand]
    private Task DismissAsync() => OnDismiss?.Invoke() ?? Task.CompletedTask;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        var chosen = Slots.FirstOrDefault(s => s.IsSelected);
        var resource = Resources.FirstOrDefault(r => r.IsSelected);

        if (chosen is null || resource?.OperatorId is not Guid operatorId || OnMove is null)
            return;

        IsSaving = true;
        try
        {
            var start = chosen.Slot.SlotStart;
            await OnMove(_booking, operatorId, start, start.AddMinutes(_service.EstMinutes));
        }
        finally
        {
            IsSaving = false;
        }
    }
}
