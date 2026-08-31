using QueueApp.Services.Location;
using QueueApp.Services.Popup;
using QueueApp.Shared.Templates.BottomSheet;

namespace QueueApp.Features.CategoryPicker.Sheets;

public partial class LocationSheet : BottomSheetPage
{
    private readonly ILocationService _locationService;
    private readonly IQueuePopupService _popupService;
    private readonly TaskCompletionSource<LocationResolution?> _completion = new();

    private LocationResolution? _latestResult;

    public string CurrentLabel { get; }
    public string UpdatedAgoText { get; }
    public bool HasCurrentLocation { get; }
    public bool IsDenied { get; private set; }
    public string PrimaryButtonText { get; private set; }
    public bool IsRefreshing { get; private set; }

    public Task<LocationResolution?> Completion => _completion.Task;

    public LocationSheet() : this(null!, null!, "", "", false, false, false)
    {
    }

    public LocationSheet(
        ILocationService locationService,
        IQueuePopupService popupService,
        string currentLabel,
        string updatedAgoText,
        bool hasCurrentLocation,
        bool isDenied,
        bool isRetryingAfterFailure)
    {
        _locationService = locationService;
        _popupService = popupService;
        CurrentLabel = currentLabel;
        UpdatedAgoText = updatedAgoText;
        HasCurrentLocation = hasCurrentLocation;
        IsDenied = isDenied;
        PrimaryButtonText = isRetryingAfterFailure ? "Try again" : "Use my current location";

        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _completion.TrySetResult(_latestResult);
    }

    private async void OnUseCurrentLocationClicked(object sender, EventArgs e)
    {
        if (IsRefreshing)
            return;

        IsRefreshing = true;
        OnPropertyChanged(nameof(IsRefreshing));
        try
        {
            var result = await _locationService.RefreshLocationAsync();
            _latestResult = result;

            if (result.Outcome is LocationOutcome.Resolved or LocationOutcome.Coarse)
            {
                _completion.TrySetResult(result);
                await _popupService.HideSheetAsync(this);
                return;
            }

            IsDenied = result.Outcome == LocationOutcome.Denied;
            PrimaryButtonText = "Try again";
            OnPropertyChanged(nameof(IsDenied));
            OnPropertyChanged(nameof(PrimaryButtonText));
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsRefreshing));
        }
    }

    private void OnOpenSettingsClicked(object sender, EventArgs e) => AppInfo.ShowSettingsUI();

    private async void OnCloseClicked(object sender, EventArgs e) => await _popupService.HideSheetAsync(this);
}
