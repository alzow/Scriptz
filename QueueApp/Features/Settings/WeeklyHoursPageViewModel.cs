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

namespace QueueApp.Features.Settings;

public class DayGroup
{
    public int DayOfWeek { get; set; }
    public string Label { get; set; } = "";
    public ObservableCollection<OperatorAvailabilityResponse> Windows { get; } = new();
}

public partial class WeeklyHoursPageViewModel : BaseViewModel
{
    private static readonly string[] DayLabels =
        { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private readonly IOperatorService _operatorService;
    private Guid _operatorId;

    public WeeklyHoursPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
    }

    public ObservableCollection<DayGroup> Days { get; } = new();
    public string OperatorName { get; set; } = "";
    public bool IsLoading { get; set; }

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
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var windows = await _operatorService.GetAvailabilityAsync(_operatorId);

            Days.Clear();
            for (var dow = 0; dow <= 6; dow++)
            {
                var group = new DayGroup { DayOfWeek = dow, Label = DayLabels[dow] };
                foreach (var w in windows.Where(w => w.DayOfWeek == dow))
                    group.Windows.Add(w);
                Days.Add(group);
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
    private async Task AddWindowAsync(int dayOfWeek)
    {
        await NavigationService.NavigateAsync(NavigationPaths.AddAvailabilityWindowPage,
            new NavigationParameters { [NavigationKeys.OperatorId] = _operatorId, [NavigationKeys.DayOfWeek] = dayOfWeek });
    }

    [RelayCommand]
    private async Task DeleteWindowAsync(OperatorAvailabilityResponse window)
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
    private async Task GoToBlockedDatesAsync()
    {
        await NavigationService.NavigateAsync(NavigationPaths.BlockedDatesPage,
            new NavigationParameters { [NavigationKeys.OperatorId] = _operatorId, [NavigationKeys.OperatorName] = OperatorName });
    }

    [RelayCommand]
    private async Task GoBackAsync()
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
