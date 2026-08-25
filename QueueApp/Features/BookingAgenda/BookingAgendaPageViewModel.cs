using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Booking;
using QueueApp.Services.Api.Booking.Models;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Auth;
using QueueApp.Services.Realtime;
using QueueApp.Services.Storage;

namespace QueueApp.Features.BookingAgenda;

public partial class BookingAgendaPageViewModel : BaseViewModel
{
    private readonly IBusinessService _businessService;
    private readonly IBookingService _bookingService;
    private readonly IQueueRealtimeService _realtimeService;
    private Guid _businessId;

    public BookingAgendaPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IBusinessService businessService,
        IBookingService bookingService,
        IQueueRealtimeService realtimeService)
        : base(navigationService, secureStorageService)
    {
        _businessService = businessService;
        _bookingService = bookingService;
        _realtimeService = realtimeService;
    }

    public List<AgendaDateOption> DateOptions { get; } =
        Enumerable.Range(0, 14)
            .Select(i => new AgendaDateOption(DateTime.Today.AddDays(i)) { IsSelected = i == 0 })
            .ToList();
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    public string BusinessName { get; set; } = "Bookings";
    public ObservableCollection<AgendaBookingResponse> Bookings { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => Bookings.Count == 0 && !IsLoading;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            var business = await _businessService.GetBusinessAsync(_businessId);
            BusinessName = business?.Name ?? "Bookings";

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
        await _realtimeService.SubscribeAsync("business_id", _businessId.ToString(),
            async () => await MainThread.InvokeOnMainThreadAsync(LoadAsync),
            table: "bookings");
    }

    public override async Task OnDisappearingAsync()
    {
        await base.OnDisappearingAsync();
        await _realtimeService.UnsubscribeAsync();
    }

    [RelayCommand]
    private async Task SelectDateAsync(AgendaDateOption option)
    {
        foreach (var d in DateOptions)
            d.IsSelected = d == option;

        SelectedDate = option.Date;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var bookings = await _bookingService.GetAgendaBookingsAsync(_businessId, SelectedDate);
            Bookings.Clear();
            foreach (var b in bookings)
            {
                b.IsConfirming = false;
                b.IsCompleting = false;
                b.IsCancelling = false;
                Bookings.Add(b);
            }
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

    [RelayCommand]
    private async Task ConfirmAsync(AgendaBookingResponse booking)
    {
        booking.IsConfirming = true;
        try { await _bookingService.ConfirmBookingAsync(booking.Id); }
        catch (Exception ex) { await HandleExceptionAsync(ex); }
        finally { booking.IsConfirming = false; }
    }

    [RelayCommand]
    private async Task CompleteAsync(AgendaBookingResponse booking)
    {
        booking.IsCompleting = true;
        try { await _bookingService.CompleteBookingAsync(booking.Id); }
        catch (Exception ex) { await HandleExceptionAsync(ex); }
        finally { booking.IsCompleting = false; }
    }

    [RelayCommand]
    private async Task CancelAsync(AgendaBookingResponse booking)
    {
        booking.IsCancelling = true;
        try { await _bookingService.CancelBookingAsync(booking.Id); }
        catch (Exception ex) { await HandleExceptionAsync(ex); }
        finally { booking.IsCancelling = false; }
    }

    [RelayCommand]
    private async Task SaveProgressAsync(AgendaBookingResponse booking)
    {
        booking.IsSavingProgress = true;
        try { await _bookingService.SetBookingProgressAsync(booking.Id, booking.ProgressStatus); }
        catch (Exception ex) { await HandleExceptionAsync(ex); }
        finally { booking.IsSavingProgress = false; }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BusinessSettingsPage);
    }
}
