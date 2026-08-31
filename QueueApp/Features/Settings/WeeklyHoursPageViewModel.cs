using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;
using QueueApp.Shared.Domain;

namespace QueueApp.Features.Settings;

public class DayGroup
{
    public int DayOfWeek { get; set; }
    public string Label { get; set; } = "";
    public ObservableCollection<OperatorAvailabilityResponse> Windows { get; } = new();
    public bool IsOpen => Windows.Count > 0;
}

public partial class WeeklyHoursPageViewModel : BaseViewModel
{
    private static readonly string[] DayLabels =
        { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private static readonly TimeSpan DefaultOpen = new(8, 0, 0);
    private static readonly TimeSpan DefaultClose = new(18, 0, 0);

    public ObservableCollection<DayGroup> Days { get; } = new();
    public string OperatorName { get; set; } = "";
    public string SummaryText { get; set; } = "";
    public bool IsLoading { get; set; }

    private Guid _operatorId;

    private readonly IOperatorService _operatorService;

    public WeeklyHoursPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _operatorId = parameters is not null && parameters.TryGetValue(NavigationKeys.OperatorId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("WeeklyHoursPage requires an operatorId.");

            OperatorName = parameters!.TryGetValue(NavigationKeys.OperatorName, out var nameObj) ? (string)nameObj : "";

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
        if (_operatorId != Guid.Empty)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var windows = await _operatorService.GetAvailabilityAsync(_operatorId);

            Days.Clear();
            for (var dow = 0; dow <= 6; dow++)
            {
                var group = new DayGroup { DayOfWeek = dow, Label = DayLabels[dow] };
                foreach (var w in windows.Where(w => w.DayOfWeek == dow).OrderBy(w => w.StartTime))
                    group.Windows.Add(w);
                Days.Add(group);
            }

            var hours = BusinessHours.FromAvailability(windows);
            SummaryText = hours.HasData ? hours.SummaryText : "Closed every day";
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
    public async Task ToggleDayOpenAsync(DayGroup day)
    {
        try
        {
            if (day.IsOpen)
            {
                foreach (var window in day.Windows.ToList())
                    await _operatorService.DeleteAvailabilityAsync(window.Id);
            }
            else
            {
                await _operatorService.CreateAvailabilityAsync(new CreateAvailabilityRequest
                {
                    OperatorId = _operatorId,
                    DayOfWeek = day.DayOfWeek,
                    StartTime = DefaultOpen,
                    EndTime = DefaultClose,
                });
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task CopyToAllDaysAsync(DayGroup source)
    {
        try
        {
            foreach (var day in Days.Where(d => d.DayOfWeek != source.DayOfWeek))
            {
                foreach (var window in day.Windows.ToList())
                    await _operatorService.DeleteAvailabilityAsync(window.Id);

                foreach (var window in source.Windows)
                {
                    await _operatorService.CreateAvailabilityAsync(new CreateAvailabilityRequest
                    {
                        OperatorId = _operatorId,
                        DayOfWeek = day.DayOfWeek,
                        StartTime = window.StartTime,
                        EndTime = window.EndTime,
                    });
                }
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task AddWindowAsync(int dayOfWeek)
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddAvailabilityWindowPage,
                new NavigationParameters { [NavigationKeys.OperatorId] = _operatorId, [NavigationKeys.DayOfWeek] = dayOfWeek });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task DeleteWindowAsync(OperatorAvailabilityResponse window)
    {
        window.IsDeleting = true;
        try
        {
            await _operatorService.DeleteAvailabilityAsync(window.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            window.IsDeleting = false;
        }
    }

    [RelayCommand]
    public async Task GoToBlockedDatesAsync()
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.BlockedDatesPage,
                new NavigationParameters { [NavigationKeys.OperatorId] = _operatorId, [NavigationKeys.OperatorName] = OperatorName });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
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
}
