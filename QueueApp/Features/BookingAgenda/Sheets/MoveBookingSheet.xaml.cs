using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Features.BusinessDetail.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.BookingAgenda.Sheets;

public partial class MoveBookingSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly IBookingService _bookingService;
    private readonly TaskCompletionSource<MoveBookingResult> _completion = new();
    private readonly ServiceResponse? _service;
    private readonly Guid _businessId;

    private DateTime _date;

    public string TitleText { get; }
    public string CurrentText { get; }
    public string ServiceText { get; }
    public DateTime MinimumDate { get; } = LocalTime.Now.Date;

    public ObservableCollection<BayFilterOption> Resources { get; } = new();
    public ObservableCollection<SlotChoiceItem> Slots { get; } = new();

    public ICommand SelectResourceCommand { get; }
    public ICommand SelectSlotCommand { get; }

    public bool IsLoadingSlots { get; private set; }
    public bool HasSlots => Slots.Count > 0;
    public bool IsEmpty => !IsLoadingSlots && Slots.Count == 0;

    public Task<MoveBookingResult> Completion => _completion.Task;

    public DateTime Date
    {
        get => _date;
        set
        {
            if (_date == value) return;
            _date = value;
            OnPropertyChanged();
            _ = LoadAsync();
        }
    }

    public MoveBookingSheet()
        : this(null!, null!, null!, null!, [], DateTime.Today, default)
    {
    }

    public MoveBookingSheet(
        IQueuePopupService popups,
        IBookingService bookingService,
        AgendaBookingResponse booking,
        ServiceResponse service,
        IReadOnlyList<OperatorResponse> operators,
        DateTime day,
        Guid businessId)
    {
        _popups = popups;
        _bookingService = bookingService;
        _service = service;
        _businessId = businessId;
        _date = day;

        TitleText = booking is null ? "Move booking" : $"Move {booking.CustomerName}";
        CurrentText = booking is null ? string.Empty : $"Now {booking.DayAndRangeDisplay}";
        ServiceText = service is null ? string.Empty : $"{service.Name} · {service.EstMinutes} min";

        foreach (var resource in operators)
            Resources.Add(new BayFilterOption
            {
                OperatorId = resource.Id,
                Label = resource.DisplayName,
                IsSelected = resource.Id == booking?.OperatorId,
            });

        if (Resources.Count > 0 && !Resources.Any(r => r.IsSelected))
            Resources[0].IsSelected = true;

        SelectResourceCommand = new RelayCommand<BayFilterOption>(SelectResource);
        SelectSlotCommand = new RelayCommand<SlotChoiceItem>(SelectSlot);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new MoveBookingResult(false));
    }

    public async Task LoadAsync()
    {
        try
        {
            SetLoading(true);

            var resource = Resources.FirstOrDefault(r => r.IsSelected);

            var slots = resource?.OperatorId is { } operatorId
                ? await _bookingService.GetAvailableSlotsAsync(operatorId, _service!.Id, Date)
                : await _bookingService.GetAvailableSlotsAnyAsync(_businessId, _service!.Id, Date);

            Slots.Clear();

            foreach (var slot in slots)
                Slots.Add(new SlotChoiceItem
                {
                    Slot = slot,
                    TimeText = LocalTime.ToLocal(slot.SlotStart).ToString("HH:mm"),
                });
        }
        catch (Exception)
        {
            Slots.Clear();
        }
        finally
        {
            SetLoading(false);
        }
    }

    public void SelectResource(BayFilterOption? resource)
    {
        try
        {
            if (resource is null)
                return;

            foreach (var option in Resources)
                option.IsSelected = ReferenceEquals(option, resource);

            _ = LoadAsync();
        }
        catch (Exception)
        {
        }
    }

    public void SelectSlot(SlotChoiceItem? slot)
    {
        try
        {
            if (slot is null)
                return;

            foreach (var option in Slots)
                option.IsSelected = ReferenceEquals(option, slot);
        }
        catch (Exception)
        {
        }
    }

    private void SetLoading(bool loading)
    {
        IsLoadingSlots = loading;
        OnPropertyChanged(nameof(IsLoadingSlots));
        OnPropertyChanged(nameof(HasSlots));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var chosen = Slots.FirstOrDefault(s => s.IsSelected);
            var resource = Resources.FirstOrDefault(r => r.IsSelected);

            if (chosen is null || resource?.OperatorId is not { } operatorId || _service is null)
            {
                await _popups.ShowAlertAsync("Pick a time", "Choose a resource and a free slot to move this to.");
                return;
            }

            var start = chosen.Slot.SlotStart;
            Close(new MoveBookingResult(true, operatorId, start, start.AddMinutes(_service.EstMinutes)));
        }
        catch (Exception)
        {
            Close(new MoveBookingResult(false));
        }
    }

    private void Close(MoveBookingResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }
}
