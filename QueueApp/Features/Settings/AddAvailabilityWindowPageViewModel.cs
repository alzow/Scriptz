using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class AddAvailabilityWindowPageViewModel : BaseViewModel
{
    private readonly IOperatorService _operatorService;
    private Guid _operatorId;
    private int _dayOfWeek;

    public AddAvailabilityWindowPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        Title = "Add Time Window";
    }

    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public bool IsSaving { get; set; }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _operatorId = parameters is not null && parameters.TryGetValue("operatorId", out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("AddAvailabilityWindowPage requires an operatorId.");

            _dayOfWeek = parameters!.TryGetValue("dayOfWeek", out var dowObj) ? (int)dowObj : 0;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (EndTime <= StartTime)
        {
            await HandleExceptionAsync(new InvalidOperationException("End time must be after start time."));
            return;
        }

        IsSaving = true;
        try
        {
            await _operatorService.CreateAvailabilityAsync(new CreateAvailabilityRequest
            {
                OperatorId = _operatorId,
                DayOfWeek = _dayOfWeek,
                StartTime = StartTime,
                EndTime = EndTime
            });
            await NavigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
