using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MPowerKit;
using MPowerKit.Navigation;
using QueueApp.Constants;
using QueueApp.Features.Settings.Models;
using QueueApp.Framework.Base;
using QueueApp.Services.Api.Business;
using QueueApp.Services.Api.Operator;
using QueueApp.Services.Api.Operator.Models;
using QueueApp.Services.Storage;

namespace QueueApp.Features.Settings;

public partial class StaffManagementPageViewModel : BaseViewModel
{
    public ObservableCollection<StaffRow> ActiveStaff { get; } = new();
    public ObservableCollection<StaffRow> InactiveStaff { get; } = new();
    public bool IsLoading { get; set; }
    public bool IsEmpty => ActiveStaff.Count == 0 && InactiveStaff.Count == 0 && !IsLoading;
    public bool HasInactive => InactiveStaff.Count > 0;
    public bool IsInactiveExpanded { get; set; }
    public string InactiveHeaderText { get; set; } = string.Empty;
    public string InactiveChevron => IsInactiveExpanded ? "ic_chevron_up" : "ic_chevron_down";

    private Guid _businessId;

    private readonly IOperatorService _operatorService;
    private readonly IBusinessService _businessService;

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
        await base.OnAppearingAsync();
        if (_businessId != Guid.Empty)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var operators = await _operatorService.GetAllOperatorsForManagementAsync(_businessId);

            ActiveStaff.Clear();
            InactiveStaff.Clear();
            foreach (var op in operators.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
                ActiveStaff.Add(new StaffRow(op));
            foreach (var op in operators.Where(o => !o.IsActive).OrderBy(o => o.SortOrder))
                InactiveStaff.Add(new StaffRow(op));

            InactiveHeaderText = $"Not on the team right now ({InactiveStaff.Count})";
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
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditOperatorPage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task EditOperatorAsync(StaffRow row)
    {
        try
        {
            await NavigationService.NavigateAsync(NavigationPaths.AddEditOperatorPage,
                new NavigationParameters { [NavigationKeys.BusinessId] = _businessId, [NavigationKeys.OperatorId] = row.Id });
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [RelayCommand]
    public async Task ReactivateAsync(StaffRow row)
    {
        row.IsReactivating = true;
        try
        {
            await _operatorService.SetOperatorActiveAsync(row.Id, true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        finally
        {
            row.IsReactivating = false;
        }
    }

    public async Task ReorderAsync(StaffRow moved, int targetIndex)
    {
        try
        {
            var fromIndex = ActiveStaff.IndexOf(moved);
            if (fromIndex < 0 || targetIndex < 0 || targetIndex >= ActiveStaff.Count || fromIndex == targetIndex)
                return;

            ActiveStaff.Move(fromIndex, targetIndex);

            for (var i = 0; i < ActiveStaff.Count; i++)
                ActiveStaff[i].SortOrder = i;

            await Task.WhenAll(ActiveStaff.Select(row => _operatorService.UpdateOperatorAsync(row.Id,
                new UpdateOperatorRequest { DisplayName = row.DisplayName, SortOrder = row.SortOrder })));
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
            await LoadAsync();
        }
    }

    [RelayCommand]
    public void ToggleInactiveExpanded()
    {
        IsInactiveExpanded = !IsInactiveExpanded;
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
