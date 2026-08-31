using CommunityToolkit.Mvvm.Input;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class AddAvailabilityBlockPageViewModel : BaseViewModel
{
    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

    public DateTime MinimumDate { get; } = DateTime.Today;
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);
    public bool IsAllDay { get; set; } = true;
    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(17, 0, 0);
    public string Reason { get; set; } = "";
    public bool IsSaving { get; set; }

    private Guid _operatorId;

    private readonly IOperatorService _operatorService;

    public AddAvailabilityBlockPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        Title = "Block a Date";
    }

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

            _operatorId = parameters is not null && parameters.TryGetValue(NavigationKeys.OperatorId, out var idObj)
                ? (Guid)idObj
                : throw new InvalidOperationException("AddAvailabilityBlockPage requires an operatorId.");
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

    [RelayCommand]
    public async Task SaveAsync()
    {
        DateTimeOffset startsAt, endsAt;

        if (IsAllDay)
        {
            startsAt = new DateTimeOffset(Date.Date, SastOffset);
            endsAt = new DateTimeOffset(Date.Date.AddDays(1), SastOffset);
        }
        else
        {
            startsAt = new DateTimeOffset(Date.Date.Add(StartTime), SastOffset);
            endsAt = new DateTimeOffset(Date.Date.Add(EndTime), SastOffset);

            if (endsAt <= startsAt)
            {
                await HandleExceptionAsync(new InvalidOperationException("End time must be after start time."));
                return;
            }
        }

        IsSaving = true;
        try
        {
            await _operatorService.CreateAvailabilityBlockAsync(new CreateAvailabilityBlockRequest
            {
                OperatorId = _operatorId,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason
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
