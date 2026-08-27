using System.Collections.ObjectModel;
using QueueApp.Features.BookingAgenda.Models;
using QueueApp.Features.BusinessDetail.Flow;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Api.ServiceOfferings.Models;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.BookingAgenda.Sheets;

public partial class AddBookingSheet : BottomSheetPage
{
    private readonly IQueuePopupService _popups;
    private readonly TaskCompletionSource<AddBookingResult> _completion = new();
    private readonly DateTimeOffset _windowStart;

    public string WindowText { get; }
    public string ResourceLabel { get; }
    public string StatusText => "CONFIRMED";

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SlotText { get; set; } = string.Empty;

    public ObservableCollection<AddBookingServiceOption> Services { get; } = new();
    public ObservableCollection<BayFilterOption> Resources { get; } = new();

    public Task<AddBookingResult> Completion => _completion.Task;

    public AddBookingSheet()
        : this(null!, default, default, Array.Empty<ServiceResponse>(), Array.Empty<OperatorResponse>(),
            CategoryLabels.Resolve(null))
    {
    }

    public AddBookingSheet(
        IQueuePopupService popups,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        IReadOnlyList<ServiceResponse> services,
        IReadOnlyList<OperatorResponse> operators,
        CategoryLabelSet labels)
    {
        _popups = popups;
        _windowStart = windowStart;

        WindowText = $"{windowStart:ddd d} · {windowStart:HH:mm} – {windowEnd:HH:mm} free";
        ResourceLabel = labels.SectionTitle.ToUpperInvariant();

        var windowMinutes = (int)(windowEnd - windowStart).TotalMinutes;

        foreach (var service in services)
            Services.Add(AddBookingServiceOption.From(service, windowMinutes));

        foreach (var resource in operators)
            Resources.Add(new BayFilterOption { OperatorId = resource.Id, Label = resource.DisplayName });

        var firstFitting = Services.FirstOrDefault(s => s.Fits);
        if (firstFitting is not null)
            SelectService(firstFitting);

        var firstResource = Resources.FirstOrDefault();
        if (firstResource is not null)
            firstResource.IsSelected = true;

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(new AddBookingResult(false));
    }

    public void SelectService(AddBookingServiceOption option)
    {
        foreach (var service in Services)
            service.IsSelected = service == option;

        SlotText = $"{_windowStart:HH:mm} – {_windowStart.AddMinutes(option.Service.EstMinutes):HH:mm}";
    }

    private void OnServiceTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: AddBookingServiceOption option } && option.Fits)
            SelectService(option);
    }

    private void OnResourceTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: BayFilterOption chosen })
            return;

        foreach (var resource in Resources)
            resource.IsSelected = resource == chosen;
    }

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        try
        {
            var service = Services.FirstOrDefault(s => s.IsSelected && s.Fits);
            var resource = Resources.FirstOrDefault(r => r.IsSelected);

            if (string.IsNullOrWhiteSpace(CustomerName) || service is null || resource?.OperatorId is null)
            {
                await _popups.ShowAlertAsync(
                    "Not quite ready",
                    "A name, a service that fits and a resource are all needed before this can be added.");
                return;
            }

            Close(new AddBookingResult(
                true,
                CustomerName.Trim(),
                string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                service.Service,
                resource.OperatorId.Value,
                _windowStart,
                _windowStart.AddMinutes(service.Service.EstMinutes)));
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
