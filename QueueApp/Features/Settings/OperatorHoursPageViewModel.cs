using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using MPowerKit.Navigation.Interfaces;
using QueueApp.Constants;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class OperatorHoursPageViewModel : BaseViewModel
{
    private readonly IOperatorService _operatorService;
    private readonly IBusinessService _businessService;

    public OperatorHoursPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        _businessService = businessService;
        Title = "Hours";
    }

    public const int SkeletonRowCount = 4;

    public List<OperatorResponse> Operators { get; set; } = new();

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            await RunFirstLoadAsync(FetchAsync);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override Task ReloadAsync() => RunFirstLoadAsync(FetchAsync);

    public async Task FetchAsync()
    {
        var businessId = await _businessService.GetOwnedBusinessIdAsync();
        var operators = await _operatorService.GetOperatorsAsync(businessId);

        if (operators.Count == 1)
        {
            await NavigationService.NavigateAsync(NavigationPaths.WeeklyHoursPage,
                new NavigationParameters { [NavigationKeys.OperatorId] = operators[0].Id, [NavigationKeys.OperatorName] = operators[0].DisplayName });
            return;
        }

        Operators = operators;
    }

    [RelayCommand]
    private async Task SelectOperatorAsync(OperatorResponse op)
    {
        await NavigationService.NavigateAsync(NavigationPaths.WeeklyHoursPage,
            new NavigationParameters { [NavigationKeys.OperatorId] = op.Id, [NavigationKeys.OperatorName] = op.DisplayName });
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
