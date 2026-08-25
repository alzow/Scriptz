using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using Newtonsoft.Json;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Queue;
using QueueApp.Services.Api.Queue.Models;
using QueueApp.Services.Auth;
using QueueApp.Services.Storage;

namespace QueueApp.Features.History;

public partial class HistoryPageViewModel : BaseViewModel
{
    private readonly IQueueService _queueService;
    private readonly IBookingService _bookingService;
    private readonly IAuthService _authService;

    public HistoryPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IQueueService queueService,
        IBookingService bookingService,
        IAuthService authService)
        : base(navigationService, secureStorageService)
    {
        _queueService = queueService;
        _bookingService = bookingService;
        _authService = authService;
    }

    public ObservableCollection<VisitResponse> Visits { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => Visits.Count == 0 && !IsLoading;

    public ObservableCollection<UpcomingBookingResponse> UpcomingBookings { get; } = new();
    public bool IsLoadingUpcoming { get; set; }
    public bool HasUpcoming => UpcomingBookings.Count > 0;

    private bool _isLoaded;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        // OnLoadedAsync already fetches on first navigation; avoid a duplicate race on initial appear.
        if (!_isLoaded)
            return;

        await RefreshVisitsAsync();
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        await base.OnLoadedAsync(parameters);
        _isLoaded = true;
        await RefreshVisitsAsync();
    }

    private async Task RefreshVisitsAsync()
    {
        if (!await _refreshLock.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            IsLoadingUpcoming = true;

            var userId = await _authService.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("No signed-in user id — should never happen post-splash-gate.");

            var customerId = Guid.Parse(userId);

            var visits = await _queueService.GetMyVisitsAsync(customerId);
            Debug.WriteLine($"[History] fetched {visits.Count} visits for user {JsonConvert.SerializeObject(visits)}");

            Visits.Clear();
            foreach (var visit in visits)
                Visits.Add(visit);

            var upcoming = await _bookingService.GetMyUpcomingBookingsAsync(customerId);

            UpcomingBookings.Clear();
            foreach (var booking in upcoming)
                UpcomingBookings.Add(booking);
            OnPropertyChanged(nameof(HasUpcoming));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsLoading = false;
            IsLoadingUpcoming = false;
            _refreshLock.Release();
        }
    }

    [RelayCommand]
    private async Task CancelUpcomingBookingAsync(UpcomingBookingResponse booking)
    {
        booking.IsCancelling = true;
        try
        {
            await _bookingService.CancelBookingAsync(booking.Id);
            UpcomingBookings.Remove(booking);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            booking.IsCancelling = false;
        }
    }
}
