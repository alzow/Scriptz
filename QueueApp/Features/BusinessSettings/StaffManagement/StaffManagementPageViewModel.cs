using System.Collections.ObjectModel;
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

namespace QueueApp.Features.BusinessSettings.StaffManagement;

public partial class StaffManagementPageViewModel : BaseViewModel
{
    private readonly IOperatorService _operatorService;
    private readonly IBusinessService _businessService;
    private Guid _businessId;

    public StaffManagementPageViewModel(
        INavigationService navigationService,
        ISecureStorageService secureStorageService,
        IOperatorService operatorService,
        IBusinessService businessService)
        : base(navigationService, secureStorageService)
    {
        _operatorService = operatorService;
        _businessService = businessService;
        Title = "Staff";
    }

    public ObservableCollection<OperatorResponse> Operators { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsAddingOperator { get; set; }
    public bool IsEmpty => Operators.Count == 0 && !IsLoading;

    public override async Task OnLoadedAsync(INavigationParameters? parameters)
    {
        try
        {
            await base.OnLoadedAsync(parameters);
            _businessId = await _businessService.GetOwnedBusinessIdAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    public override async Task OnAppearingAsync()
    {
        try
        {
            await base.OnAppearingAsync();
            if (_businessId != Guid.Empty)
                await LoadAsync();
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var operators = await _operatorService.GetAllOperatorsForManagementAsync(_businessId);
            Operators.Clear();
            foreach (var o in operators)
                Operators.Add(o);
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
    public async Task AddOperatorAsync()
    {
        IsAddingOperator = true;
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditOperatorPage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            IsAddingOperator = false;
        }
    }

    [RelayCommand]
    public async Task EditOperatorAsync(OperatorResponse op)
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditOperatorPage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId, [NavigationKeys.OperatorId] = op.Id });
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(exception);
        }
    }

    [RelayCommand]
    public async Task ToggleActiveAsync(OperatorResponse op)
    {
        op.IsToggling = true;
        try
        {
            await _operatorService.SetOperatorActiveAsync(op.Id, !op.IsActive);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            op.IsToggling = false;
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
