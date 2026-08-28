using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Shared.Domain;
using QueueApp.Shared.Domain.Models;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.BookingAgenda.Sheets;

public partial class AddBookingSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly IBookingService _bookingService;
    private readonly TaskCompletionSource<AddBookingResult> _completion = new();
    private readonly Guid _businessId;

    private DateTime _date;

    public string ResourceLabel { get; }
    public DateTime MinimumDate { get; } = LocalTime.Now.Date;
    public string StatusText => "CONFIRMED";

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public ObservableCollection<AddBookingServiceOption> Services { get; } = new();
    public ObservableCollection<BayFilterOption> Resources { get; } = new();
    public ObservableCollection<SlotChoiceItem> Slots { get; } = new();

    public ICommand SelectServiceCommand { get; }
    public ICommand SelectResourceCommand { get; }
    public ICommand SelectSlotCommand { get; }

    public bool IsLoadingSlots { get; private set; }
    public bool HasSlots => Slots.Count > 0;
    public bool IsEmpty => !IsLoadingSlots && Slots.Count == 0;
    public string SlotText { get; private set; } = string.Empty;
    public bool HasSlotText => SlotText.Length > 0;

    public DateTimeOffset? PreferredStart { get; set; }

    public Task<AddBookingResult> Completion => _completion.Task;

    public DateTime Date
    {
        get => _date;
        set
        {
            if (_date == value) return;
            _date = value;
            OnPropertyChanged();
            _ = LoadSlotsAsync();
        }
    }

    public AddBookingSheet()
        : this(null!, null!, default, DateTime.Today, [], [], CategoryLabels.Resolve(null))
    {
    }

    public AddBookingSheet(
        IQueuePopupService popups,
        IBookingService bookingService,
        Guid businessId,
        DateTime day,
        IReadOnlyList<ServiceResponse> services,
        IReadOnlyList<OperatorResponse> operators,
        CategoryLabelSet labels)
    {
        _popups = popups;
        _bookingService = bookingService;
        _businessId = businessId;
        _date = day;

        ResourceLabel = labels.SectionTitle.ToUpperInvariant();

        foreach (var service in services)
            Services.Add(AddBookingServiceOption.From(service));

        foreach (var resource in operators)
            Resources.Add(new BayFilterOption { OperatorId = resource.Id, Label = resource.DisplayName });

        if (Services.Count > 0)
            Services[0].IsSelected = true;

        if (Resources.Count > 0)
            Resources[0].IsSelected = true;

        SelectServiceCommand = new RelayCommand<AddBookingServiceOption>(SelectService);
        SelectResourceCommand = new RelayCommand<BayFilterOption>(SelectResource);
        SelectSlotCommand = new RelayCommand<SlotChoiceItem>(SelectSlot);

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new AddBookingResult(false));
    }

    public async Task LoadSlotsAsync()
    {
        try
        {
            var service = SelectedService();

            if (service is null)
                return;

            SetLoading(true);
            Slots.Clear();
            SetSlotText(string.Empty);

            var resource = Resources.FirstOrDefault(r => r.IsSelected);

            var slots = resource?.OperatorId is { } operatorId
                ? await _bookingService.GetAvailableSlotsAsync(operatorId, service.Service.Id, Date)
                : await _bookingService.GetAvailableSlotsAnyAsync(_businessId, service.Service.Id, Date);

            foreach (var slot in slots)
                Slots.Add(new SlotChoiceItem
                {
                    Slot = slot,
                    TimeText = LocalTime.ToLocal(slot.SlotStart).ToString("HH:mm"),
                });

            var preferred = PreferredStart is { } wanted
                ? Slots.FirstOrDefault(s => s.Slot.SlotStart == wanted)
                : null;

            if (preferred is not null)
                SelectSlot(preferred);

            PreferredStart = null;
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

    public void SelectService(AddBookingServiceOption? option)
    {
        try
        {
            if (option is null)
                return;

            foreach (var service in Services)
                service.IsSelected = ReferenceEquals(service, option);

            _ = LoadSlotsAsync();
        }
        catch (Exception)
        {
        }
    }

    public void SelectResource(BayFilterOption? option)
    {
        try
        {
            if (option is null)
                return;

            foreach (var resource in Resources)
                resource.IsSelected = ReferenceEquals(resource, option);

            _ = LoadSlotsAsync();
        }
        catch (Exception)
        {
        }
    }

    public void SelectSlot(SlotChoiceItem? item)
    {
        try
        {
            if (item is null)
                return;

            foreach (var slot in Slots)
                slot.IsSelected = ReferenceEquals(slot, item);

            var service = SelectedService();

            if (service is null)
                return;

            var start = LocalTime.ToLocal(item.Slot.SlotStart);
            SetSlotText($"{start:ddd d} · {start:HH:mm} – {start.AddMinutes(service.Service.EstMinutes):HH:mm}");
        }
        catch (Exception)
        {
        }
    }

    public AddBookingServiceOption? SelectedService() => Services.FirstOrDefault(s => s.IsSelected);

    private void SetLoading(bool loading)
    {
        IsLoadingSlots = loading;
        OnPropertyChanged(nameof(IsLoadingSlots));
        OnPropertyChanged(nameof(HasSlots));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void SetSlotText(string text)
    {
        SlotText = text;
        OnPropertyChanged(nameof(SlotText));
        OnPropertyChanged(nameof(HasSlotText));
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var service = SelectedService();
            var resource = Resources.FirstOrDefault(r => r.IsSelected);
            var slot = Slots.FirstOrDefault(s => s.IsSelected);

            if (string.IsNullOrWhiteSpace(CustomerName) || service is null
                || resource?.OperatorId is not { } operatorId || slot is null)
            {
                await _popups.ShowAlertAsync(
                    "Not quite ready",
                    "A name, a service, a resource and a free time are all needed before this can be added.");
                return;
            }

            var start = slot.Slot.SlotStart;

            Close(new AddBookingResult(
                true,
                CustomerName.Trim(),
                string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                service.Service,
                operatorId,
                start,
                start.AddMinutes(service.Service.EstMinutes),
                string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()));
        }
        catch (Exception)
        {
            Close(new AddBookingResult(false));
        }
    }

    private void Close(AddBookingResult result)
    {
        _completion.TrySetResult(result);
        _ = _popups.HideSheetAsync(this);
    }
}
