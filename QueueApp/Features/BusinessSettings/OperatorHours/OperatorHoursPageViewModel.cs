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

namespace QueueApp.Features.BusinessSettings.OperatorHours;

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

    public List<OperatorResponse> Operators { get; set; } = new();
    public bool IsLoading { get; set; } = true;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);

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
    public async Task SelectOperatorAsync(OperatorResponse op)
    {
        await NavigationService.NavigateAsync(NavigationPaths.WeeklyHoursPage,
            new NavigationParameters { [NavigationKeys.OperatorId] = op.Id, [NavigationKeys.OperatorName] = op.DisplayName });
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
